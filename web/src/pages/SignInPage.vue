<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { googleSignInUrl } from '../api/client'

const messages: Record<string, string> = {
  'not-a-member': "That account isn't on the volunteer roster.",
  inactive: 'Your membership is inactive.',
  'email-not-verified': 'Google has not verified that email address.',
}

const route = useRoute()
const error = computed(() => {
  const code = route.query.error
  return typeof code === 'string' ? (messages[code] ?? 'Sign-in failed.') : null
})
</script>

<template>
  <section>
    <h1>PAS Volunteers</h1>
    <p v-if="error" role="alert">{{ error }} Please contact the volunteer coordinator.</p>
    <a class="button" :href="googleSignInUrl">Sign in with Google</a>
  </section>
</template>

<style scoped>
.button {
  display: inline-block;
  padding: 0.75rem 1.25rem;
  border-radius: 0.375rem;
  background: #1f6feb;
  color: #fff;
  text-decoration: none;
}
</style>
