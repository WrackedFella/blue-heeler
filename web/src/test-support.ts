import { flushPromises } from '@vue/test-utils'
import { vi } from 'vitest'

// Routes load their pages lazily, so a navigation settles only after the dynamic import.
export async function settle() {
  await flushPromises()
  await vi.dynamicImportSettled()
  await flushPromises()
}
