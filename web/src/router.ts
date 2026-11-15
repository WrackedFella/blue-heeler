import { createRouter, createWebHistory, type RouterHistory } from 'vue-router'
import type { Me } from './api/client'
import { currentMember } from './session'

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
      {
        path: '/members/new',
        name: 'member-new',
        component: () => import('./pages/MemberFormPage.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: '/members/:id/edit',
        name: 'member-edit',
        component: () => import('./pages/MemberFormPage.vue'),
        props: true,
        meta: { requiresAuth: true },
      },
    ],
  })

  router.beforeEach(async (to) => {
    if (!to.meta.requiresAuth) return true
    // Refreshed on every guarded navigation, and cleared when nobody is signed in.
    currentMember.value = await getMe()
    return currentMember.value ? true : { name: 'sign-in' }
  })

  return router
}
