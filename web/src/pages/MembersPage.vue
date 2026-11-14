<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError, getMembers, signOut, type Member } from '../api/client'
import { can, currentMember } from '../session'

const props = withDefaults(
  defineProps<{ load?: () => Promise<Member[]>; endSession?: () => Promise<void> }>(),
  { load: getMembers, endSession: signOut },
)

const roleLabels: Record<Member['role'], string> = { Admin: 'Admin', ShiftLead: 'Shift Lead', Walker: 'Walker' }

const router = useRouter()
const members = ref<Member[]>([])
const problem = ref<string | null>(null)
const canManage = can('ManageMembers')

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
  currentMember.value = null
  await router.push({ name: 'sign-in' })
}
</script>

<template>
  <section>
    <header class="bar">
      <h1>Members</h1>
      <div class="bar-actions">
        <RouterLink v-if="canManage" :to="{ name: 'member-new' }">Add member</RouterLink>
        <button type="button" @click="onSignOut">Sign out</button>
      </div>
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
          <th v-if="canManage" scope="col"><span class="visually-hidden">Actions</span></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="member in members" :key="member.id">
          <td>{{ member.name }}</td>
          <td>{{ member.email }}</td>
          <td>{{ roleLabels[member.role] }}</td>
          <td>{{ member.tier ?? '' }}</td>
          <td>{{ member.status }}</td>
          <td v-if="canManage">
            <RouterLink :to="{ name: 'member-edit', params: { id: member.id } }">Edit</RouterLink>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<style scoped>
.bar-actions {
  display: flex;
  gap: 1rem;
  align-items: center;
}
.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip-path: inset(50%);
}
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
