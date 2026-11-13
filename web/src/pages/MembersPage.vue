<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError, getMembers, signOut, type Member } from '../api/client'

const props = withDefaults(
  defineProps<{ load?: () => Promise<Member[]>; endSession?: () => Promise<void> }>(),
  { load: getMembers, endSession: signOut },
)

const roleLabels: Record<Member['role'], string> = { Admin: 'Admin', ShiftLead: 'Shift Lead', Walker: 'Walker' }

const router = useRouter()
const members = ref<Member[]>([])
const problem = ref<string | null>(null)

onMounted(async () => {
  try {
    members.value = await props.load()
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      await router.push({ name: 'sign-in' })
    } else if (error instanceof ApiError && error.status === 403) {
      problem.value = "You don't have access to the member list."
    } else {
      problem.value = 'The member list could not be loaded. Try again shortly.'
    }
  }
})

async function onSignOut() {
  await props.endSession()
  await router.push({ name: 'sign-in' })
}
</script>

<template>
  <section>
    <header class="bar">
      <h1>Members</h1>
      <button type="button" @click="onSignOut">Sign out</button>
    </header>
    <p v-if="problem" role="alert">{{ problem }}</p>
    <table v-else>
      <thead>
        <tr>
          <th scope="col">Name</th>
          <th scope="col">Email</th>
          <th scope="col">Role</th>
          <th scope="col">Tier</th>
          <th scope="col">Status</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="member in members" :key="member.id">
          <td>{{ member.name }}</td>
          <td>{{ member.email }}</td>
          <td>{{ roleLabels[member.role] }}</td>
          <td>{{ member.tier ?? '' }}</td>
          <td>{{ member.status }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<style scoped>
.bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
table {
  width: 100%;
  border-collapse: collapse;
}
th,
td {
  text-align: left;
  padding: 0.5rem;
  border-bottom: 1px solid #d0d7de;
}
</style>
