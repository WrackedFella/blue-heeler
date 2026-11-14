import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { createMemoryHistory } from 'vue-router'
import { ApiError, type Member, type MemberInput } from '../api/client'
import { createAppRouter } from '../router'
import { settle } from '../test-support'
import MemberFormPage, { type MemberFormApi } from './MemberFormPage.vue'

const sam: Member = { id: '7', name: 'Sam', email: 'sam@example.org', role: 'Walker', tier: 1, status: 'Active' }
const admin = { id: '1', name: 'Justin', email: 'admin@example.org', role: 'Admin', permissions: ['ViewMembers', 'ManageMembers'] }

function fakeApi(overrides: Partial<MemberFormApi> = {}) {
  const calls: { add: MemberInput[]; edit: [string, MemberInput][] } = { add: [], edit: [] }
  const api: MemberFormApi = {
    load: async () => sam,
    add: async (input) => {
      calls.add.push(input)
      return { id: '9', ...input }
    },
    edit: async (id, input) => {
      calls.edit.push([id, input])
      return { id, ...input }
    },
    ...overrides,
  }
  return { api, calls }
}

async function mountForm(props: { id?: string; api: MemberFormApi }) {
  const router = createAppRouter({ getMe: async () => admin, history: createMemoryHistory() })
  await router.push(props.id ? `/members/${props.id}/edit` : '/members/new')
  const page = mount(MemberFormPage, { props, global: { plugins: [router] } })
  await settle()
  return { page, router }
}

describe('MemberFormPage', () => {
  it('adds a member with the entered details and returns to the list', async () => {
    const { api, calls } = fakeApi()
    const { page, router } = await mountForm({ api })

    await page.find('input[name="name"]').setValue('Alex')
    await page.find('input[name="email"]').setValue('alex@example.org')
    await page.find('select[name="role"]').setValue('ShiftLead')
    await page.find('select[name="tier"]').setValue('2')
    await page.find('form').trigger('submit')
    await settle()

    expect(calls.add).toEqual([{ name: 'Alex', email: 'alex@example.org', role: 'ShiftLead', tier: 2, status: 'Active' }])
    expect(router.currentRoute.value.name).toBe('members')
  })

  it('sends no tier when None is chosen', async () => {
    const { api, calls } = fakeApi()
    const { page } = await mountForm({ api })

    await page.find('input[name="name"]').setValue('Pat')
    await page.find('input[name="email"]').setValue('pat@example.org')
    const tier = page.find('select[name="tier"]')
    const none = tier.find('option').element as HTMLOptionElement
    none.selected = true
    await tier.trigger('change')
    await page.find('form').trigger('submit')
    await settle()

    expect(calls.add[0]?.tier).toBeNull()
  })

  it('loads the member and saves changes to it', async () => {
    const { api, calls } = fakeApi()
    const { page } = await mountForm({ id: '7', api })

    expect((page.find('input[name="name"]').element as HTMLInputElement).value).toBe('Sam')
    await page.find('select[name="status"]').setValue('Inactive')
    await page.find('form').trigger('submit')
    await settle()

    expect(calls.edit).toEqual([['7', { name: 'Sam', email: 'sam@example.org', role: 'Walker', tier: 1, status: 'Inactive' }]])
  })

  it('shows field errors next to their fields and stays on the form', async () => {
    const { api } = fakeApi({
      add: () => Promise.reject(new ApiError(400, '/api/members', null, { email: ['Enter an email address.'] })),
    })
    const { page, router } = await mountForm({ api })

    await page.find('form').trigger('submit')
    await settle()

    expect(page.find('[data-field="email"]').text()).toBe('Enter an email address.')
    expect(page.find('[data-field="name"]').exists()).toBe(false)
    expect(router.currentRoute.value.name).toBe('member-new')
  })

  it('shows a conflict message from the server', async () => {
    const { api } = fakeApi({
      edit: () => Promise.reject(new ApiError(409, '/api/members/7', 'This is the only active admin. Make someone else an admin first.')),
    })
    const { page } = await mountForm({ id: '7', api })

    await page.find('form').trigger('submit')
    await settle()

    expect(page.find('[role="alert"]').text()).toContain('only active admin')
  })
})
