import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'

// In development the API runs on its own port. The proxy keeps the browser on one origin,
// so the session cookie and the Google redirect behave as they do in production.
const api = 'http://localhost:5207'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': { target: api, xfwd: true },
      '/signin-google': { target: api, xfwd: true },
    },
  },
  test: {
    environment: 'jsdom',
  },
})
