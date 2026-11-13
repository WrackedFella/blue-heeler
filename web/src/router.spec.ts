import { describe, expect, it } from 'vitest'
import { createMemoryHistory } from 'vue-router'
import type { Me } from './api/client'
import { createAppRouter } from './router'

const admin: Me = { id: '1', name: 'Justin', email: 'admin@example.org', role: 'Admin', permissions: ['ViewMembers'] }

async function landOn(path: string, me: Me | null) {
  const router = createAppRouter({ getMe: async () => me, history: createMemoryHistory() })
  await router.push(path)
  return router.currentRoute.value.name
}

describe('router', () => {
  it('sends a signed-out visitor to the sign-in page', async () => {
    expect(await landOn('/members', null)).toBe('sign-in')
  })

  it('shows the member list to a signed-in member', async () => {
    expect(await landOn('/members', admin)).toBe('members')
  })

  it('opens the member list from the root path', async () => {
    expect(await landOn('/', admin)).toBe('members')
  })

  it('shows the sign-in page without asking who is signed in', async () => {
    const router = createAppRouter({
      getMe: () => Promise.reject(new Error('must not be called')),
      history: createMemoryHistory(),
    })

    await router.push('/sign-in')

    expect(router.currentRoute.value.name).toBe('sign-in')
  })
})
