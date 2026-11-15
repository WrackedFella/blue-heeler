// Every HTTP call the SPA makes goes through this module, so a generated client can
// replace it without touching pages.

export interface Me {
  id: string
  name: string
  email: string
  role: string
  permissions: string[]
}

export type Role = 'Admin' | 'ShiftLead' | 'Walker'
export type Tier = 1 | 2 | 3
export type Status = 'Active' | 'Inactive'

export interface Member {
  id: string
  name: string
  email: string
  role: Role
  tier: Tier | null
  status: Status
}

export type MemberInput = Omit<Member, 'id'>

/** A failed call, with the server's problem details when it sent them. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    path: string,
    readonly detail: string | null = null,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(`${path} answered ${status}`)
  }
}

// Cross-site request protection (ADR 0003): every state-changing call echoes an antiforgery
// token in this header. The token is bound to who is signed in, so it is fetched lazily and
// dropped when the session changes or a call is refused, and the next call fetches a fresh one.
const tokenHeader = 'X-XSRF-TOKEN'
let antiforgeryToken: Promise<string> | null = null

function requestToken(): Promise<string> {
  antiforgeryToken ??= send<{ token: string }>('GET', '/api/antiforgery/token').then((r) => r.token)
  return antiforgeryToken
}

async function send<T>(method: string, path: string, body?: unknown): Promise<T> {
  const isRead = method === 'GET'
  const response = await fetch(path, {
    method,
    headers: {
      Accept: 'application/json',
      ...(isRead ? {} : { [tokenHeader]: await requestToken() }),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) {
    if (!isRead) antiforgeryToken = null
    throw await toError(response, path)
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

async function toError(response: Response, path: string): Promise<ApiError> {
  const isProblem = response.headers.get('Content-Type')?.includes('json') ?? false
  const problem = isProblem ? ((await response.json()) as { detail?: string; errors?: Record<string, string[]> }) : {}
  return new ApiError(response.status, path, problem.detail ?? null, problem.errors ?? {})
}

/** The signed-in member, or null when nobody is signed in. */
export async function getMe(): Promise<Me | null> {
  try {
    return await send<Me>('GET', '/api/me')
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return null
    throw error
  }
}

export const getMembers = (): Promise<Member[]> => send<Member[]>('GET', '/api/members')
export const getMember = (id: string): Promise<Member> => send<Member>('GET', `/api/members/${encodeURIComponent(id)}`)
export const addMember = (input: MemberInput): Promise<Member> => send<Member>('POST', '/api/members', input)
export const editMember = (id: string, input: MemberInput): Promise<Member> =>
  send<Member>('PUT', `/api/members/${encodeURIComponent(id)}`, input)
export async function signOut(): Promise<void> {
  await send<void>('POST', '/api/auth/sign-out')
  antiforgeryToken = null
}

export const googleSignInUrl = '/api/auth/google'
