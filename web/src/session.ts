import { ref } from 'vue'
import type { Me } from './api/client'

/** The member the router guard last saw signed in; pages read permissions from it. */
export const currentMember = ref<Me | null>(null)

export const can = (permission: string): boolean => currentMember.value?.permissions.includes(permission) ?? false
