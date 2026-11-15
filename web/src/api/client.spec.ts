import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { addMember, ApiError, getMembers, signOut } from './client'

type Call = [string, RequestInit]

// Answers the token endpoint with a fresh token each time, and every other call with `respond`.
function stubFetch(respond: () => Response) {
  let issued = 0
  const fetchMock = vi.fn(async (...[path]: Call) =>
    path === '/api/antiforgery/token' ? Response.json({ token: `token-${++issued}` }) : respond(),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

const headersOf = (init: RequestInit | undefined) => new Headers(init?.headers)
const callsTo = (fetchMock: ReturnType<typeof stubFetch>, path: string) =>
  (fetchMock.mock.calls as Call[]).filter(([p]) => p === path)
const validInput = { name: 'Sam', email: 'sam@example.org', role: 'Walker', tier: 1, status: 'Active' } as const

// Each test starts signed out so no token is cached from an earlier test.
beforeEach(async () => {
  stubFetch(() => new Response(null, { status: 204 }))
  await signOut()
})
afterEach(() => vi.unstubAllGlobals())

describe('api client', () => {
  it('sends the antiforgery token on state-changing calls', async () => {
    const fetchMock = stubFetch(() => Response.json({ id: '1' }, { status: 201 }))

    await addMember(validInput)

    expect(headersOf(callsTo(fetchMock, '/api/members')[0]?.[1]).get('X-XSRF-TOKEN')).toBe('token-1')
  })

  it('fetches the token once and reuses it', async () => {
    const fetchMock = stubFetch(() => Response.json({ id: '1' }, { status: 201 }))

    await addMember(validInput)
    await addMember(validInput)

    expect(callsTo(fetchMock, '/api/antiforgery/token')).toHaveLength(1)
  })

  it('fetches a new token after signing out', async () => {
    const fetchMock = stubFetch(() => new Response(null, { status: 204 }))

    await signOut()
    await signOut()

    expect(callsTo(fetchMock, '/api/auth/sign-out').map(([, init]) => headersOf(init).get('X-XSRF-TOKEN'))).toEqual(['token-1', 'token-2'])
  })

  it('fetches a new token after a refused call', async () => {
    let status = 400
    const fetchMock = stubFetch(() => Response.json({ id: '1' }, { status }))

    await addMember(validInput).catch(() => undefined)
    status = 201
    await addMember(validInput)

    expect(headersOf(callsTo(fetchMock, '/api/members')[1]?.[1]).get('X-XSRF-TOKEN')).toBe('token-2')
  })

  it('sends a JSON body when adding a member', async () => {
    const fetchMock = stubFetch(() => Response.json({ id: '1' }, { status: 201 }))

    await addMember(validInput)

    const [, init] = callsTo(fetchMock, '/api/members')[0]!
    expect([init.method, JSON.parse(init.body as string)]).toEqual(['POST', validInput])
    expect(headersOf(init).get('Content-Type')).toBe('application/json')
  })

  it('sends no token on reads', async () => {
    const fetchMock = stubFetch(() => Response.json([]))

    await getMembers()

    expect(fetchMock.mock.calls).toHaveLength(1)
    expect(headersOf(fetchMock.mock.calls[0]?.[1]).has('X-XSRF-TOKEN')).toBe(false)
  })

  it('turns problem details into an ApiError with field errors', async () => {
    stubFetch(() =>
      Response.json(
        { title: 'One or more validation errors occurred.', errors: { name: ['Enter a name.'] } },
        { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
      ),
    )

    const error = await addMember({ ...validInput, name: '' }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).fieldErrors).toEqual({ name: ['Enter a name.'] })
  })
})
