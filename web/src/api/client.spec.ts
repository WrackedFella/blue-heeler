import { afterEach, describe, expect, it, vi } from 'vitest'
import { addMember, ApiError, getMembers, signOut } from './client'

function stubFetch(response: Response) {
  const fetchMock = vi.fn(async (..._args: [string, RequestInit]) => response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

const headersOf = (init: RequestInit | undefined) => new Headers(init?.headers)

afterEach(() => vi.unstubAllGlobals())

describe('api client', () => {
  it('sends the cross-site request header on state-changing calls', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    await signOut()

    expect(headersOf(fetchMock.mock.calls[0]?.[1]).get('X-PAS-Request')).toBe('1')
  })

  it('sends a JSON body when adding a member', async () => {
    const fetchMock = stubFetch(Response.json({ id: '1' }, { status: 201 }))
    const input = { name: 'Sam', email: 'sam@example.org', role: 'Walker', tier: 1, status: 'Active' } as const

    await addMember(input)

    const [path, init] = fetchMock.mock.calls[0]!
    expect([path, init.method, JSON.parse(init.body as string)]).toEqual(['/api/members', 'POST', input])
    expect(headersOf(init).get('Content-Type')).toBe('application/json')
  })

  it('omits the header on reads', async () => {
    const fetchMock = stubFetch(Response.json([]))

    await getMembers()

    expect(headersOf(fetchMock.mock.calls[0]?.[1]).has('X-PAS-Request')).toBe(false)
  })

  it('turns problem details into an ApiError with field errors', async () => {
    stubFetch(
      Response.json(
        { title: 'One or more validation errors occurred.', errors: { name: ['Enter a name.'] } },
        { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
      ),
    )

    const error = await addMember({ name: '', email: 'a@b.c', role: 'Walker', tier: null, status: 'Active' }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).fieldErrors).toEqual({ name: ['Enter a name.'] })
  })
})
