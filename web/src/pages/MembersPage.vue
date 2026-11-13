<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getMembers, type Member } from '../api/client'

const props = withDefaults(defineProps<{ load?: () => Promise<Member[]> }>(), { load: getMembers })

const roleLabels: Record<Member['role'], string> = { Admin: 'Admin', ShiftLead: 'Shift Lead', Walker: 'Walker' }

const members = ref<Member[]>([])
const failed = ref(false)

onMounted(async () => {
  try {
    members.value = await props.load()
  } catch {
    failed.value = true
  }
})
</script>

<template>
  <section>
    <h1>Members</h1>
    <p v-if="failed" role="alert">The member list could not be loaded. Try again shortly.</p>
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
