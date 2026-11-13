import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { createMemoryHistory } from 'vue-router'
import { ApiError, type Member } from '../api/client'
import { createAppRouter } from '../router'
import MembersPage from './MembersPage.vue'

const justin: Member = { id: '1', name: 'Justin', email: 'admin@example.org', role: 'Admin', tier: null, status: 'Active' }
const sam: Member = { id: '2', name: 'Sam', email: 'sam@example.org', role: 'ShiftLead', tier: 2, status: 'Inactive' }

// Routes load their pages lazily, so a navigation settles only after the dynamic import.
async function settle() {
  await flushPromises()
  await vi.dynamicImportSettled()
  await flushPromises()
}

async function mountPage(props: { load: () => Promise<Member[]>; endSession?: () => Promise<void> }) {
  const me = { id: '1', name: 'Justin', email: 'admin@example.org', role: 'Admin', permissions: ['ViewMembers'] }
  const router = createAppRouter({ getMe: async () => me, history: createMemoryHistory() })
  await router.push('/members')
  const page = mount(MembersPage, { props, global: { plugins: [router] } })
  await settle()
  return { page, router }
}

describe('MembersPage', () => {
  it('lists each member with role, tier and status', async () => {
    const { page } = await mountPage({ load: async () => [justin, sam] })

    const rows = page.findAll('tbody tr').map((row) => row.findAll('td').map((cell) => cell.text()))
    expect(rows).toEqual([
      ['Justin', 'admin@example.org', 'Admin', '', 'Active'],
      ['Sam', 'sam@example.org', 'Shift Lead', '2', 'Inactive'],
    ])
  })

  it('says so when the list cannot be loaded', async () => {
    const { page } = await mountPage({ load: () => Promise.reject(new Error('down')) })

    expect(page.find('[role="alert"]').text()).toContain('could not be loaded')
    expect(page.find('table').exists()).toBe(false)
  })

  it('tells a member without the permission they have no access', async () => {
    const { page } = await mountPage({ load: () => Promise.reject(new ApiError(403, '/api/members')) })

    expect(page.find('[role="alert"]').text()).toContain("don't have access")
  })

  it('sends a member whose session ended to the sign-in page', async () => {
    const { router } = await mountPage({ load: () => Promise.reject(new ApiError(401, '/api/members')) })

    expect(router.currentRoute.value.name).toBe('sign-in')
  })

  it('signs out and returns to the sign-in page', async () => {
    let ended = false
    const { page, router } = await mountPage({
      load: async () => [justin],
      endSession: async () => {
        ended = true
      },
    })

    await page.find('button').trigger('click')
    await settle()

    expect(ended).toBe(true)
    expect(router.currentRoute.value.name).toBe('sign-in')
  })
})
