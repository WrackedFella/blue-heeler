// Every HTTP call the SPA makes goes through this module, so a generated client can
// replace it without touching pages.

export interface Me {
  id: string
  name: string
  email: string
  role: string
  permissions: string[]
}

export interface Member {
  id: string
  name: string
  email: string
  role: 'Admin' | 'ShiftLead' | 'Walker'
  tier: 1 | 2 | 3 | null
  status: 'Active' | 'Inactive'
}

export class ApiError extends Error {
  constructor(readonly status: number, path: string) {
    super(`${path} answered ${status}`)
  }
}

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(path, { headers: { Accept: 'application/json' } })
  if (!response.ok) throw new ApiError(response.status, path)
  return (await response.json()) as T
}

/** The signed-in member, or null when nobody is signed in. */
export async function getMe(): Promise<Me | null> {
  try {
    return await getJson<Me>('/api/me')
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return null
    throw error
  }
}

export const getMembers = (): Promise<Member[]> => getJson<Member[]>('/api/members')

export async function signOut(): Promise<void> {
  const response = await fetch('/api/auth/sign-out', { method: 'POST' })
  if (!response.ok) throw new ApiError(response.status, '/api/auth/sign-out')
}

export const googleSignInUrl = '/api/auth/google'
