import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import type { Member } from '../api/client'
import MembersPage from './MembersPage.vue'

const justin: Member = { id: '1', name: 'Justin', email: 'admin@example.org', role: 'Admin', tier: null, status: 'Active' }
const sam: Member = { id: '2', name: 'Sam', email: 'sam@example.org', role: 'ShiftLead', tier: 2, status: 'Inactive' }

describe('MembersPage', () => {
  it('lists each member with role, tier and status', async () => {
    const page = mount(MembersPage, { props: { load: async () => [justin, sam] } })
    await flushPromises()

    const rows = page.findAll('tbody tr').map((row) => row.findAll('td').map((cell) => cell.text()))
    expect(rows).toEqual([
      ['Justin', 'admin@example.org', 'Admin', '', 'Active'],
      ['Sam', 'sam@example.org', 'Shift Lead', '2', 'Inactive'],
    ])
  })

  it('says so when the list cannot be loaded', async () => {
    const page = mount(MembersPage, { props: { load: () => Promise.reject(new Error('down')) } })
    await flushPromises()

    expect(page.find('[role="alert"]').exists()).toBe(true)
    expect(page.find('table').exists()).toBe(false)
  })
})
