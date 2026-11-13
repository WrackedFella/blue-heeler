import { createRouter, createWebHistory, type RouterHistory } from 'vue-router'
import type { Me } from './api/client'

export interface RouterDeps {
  getMe: () => Promise<Me | null>
  history?: RouterHistory
}

export function createAppRouter({ getMe, history = createWebHistory() }: RouterDeps) {
  const router = createRouter({
    history,
    routes: [
      { path: '/', redirect: '/members' },
      { path: '/sign-in', name: 'sign-in', component: () => import('./pages/SignInPage.vue') },
      {
        path: '/members',
        name: 'members',
        component: () => import('./pages/MembersPage.vue'),
        meta: { requiresAuth: true },
      },
    ],
  })

  router.beforeEach(async (to) => {
    if (!to.meta.requiresAuth) return true
    return (await getMe()) ? true : { name: 'sign-in' }
  })

  return router
}
