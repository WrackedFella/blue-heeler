import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { createMemoryHistory } from 'vue-router'
import { createAppRouter } from '../router'
import { settle } from '../test-support'
import SignInPage from './SignInPage.vue'

async function mountAt(path: string) {
  const router = createAppRouter({ getMe: async () => null, history: createMemoryHistory() })
  await router.push(path)
  const page = mount(SignInPage, { global: { plugins: [router] } })
  await settle()
  return page
}

describe('SignInPage', () => {
  it.each(['not-a-member', 'inactive', 'email-not-verified', 'something-new'])(
    'tells a refused visitor to contact the volunteer coordinator (%s)',
    async (code) => {
      const page = await mountAt(`/sign-in?error=${code}`)

      expect(page.find('[role="alert"]').text()).toContain('contact the volunteer coordinator')
    },
  )

  it('shows no error on a plain visit', async () => {
    const page = await mountAt('/sign-in')

    expect(page.find('[role="alert"]').exists()).toBe(false)
  })
})
