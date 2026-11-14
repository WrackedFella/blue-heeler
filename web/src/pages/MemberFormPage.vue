<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  addMember,
  ApiError,
  editMember,
  getMember,
  type Member,
  type MemberInput,
  type Role,
  type Status,
  type Tier,
} from '../api/client'

export interface MemberFormApi {
  load: (id: string) => Promise<Member>
  add: (input: MemberInput) => Promise<Member>
  edit: (id: string, input: MemberInput) => Promise<Member>
}

const props = withDefaults(defineProps<{ id?: string; api?: MemberFormApi }>(), {
  id: undefined,
  api: () => ({ load: getMember, add: addMember, edit: editMember }),
})

const roles: { value: Role; label: string }[] = [
  { value: 'Walker', label: 'Walker' },
  { value: 'ShiftLead', label: 'Shift Lead' },
  { value: 'Admin', label: 'Admin' },
]
const tiers: Tier[] = [1, 2, 3]
const statuses: Status[] = ['Active', 'Inactive']

const router = useRouter()
const form = reactive<MemberInput>({ name: '', email: '', role: 'Walker', tier: 1, status: 'Active' })
const fieldErrors = ref<Record<string, string[]>>({})
const problem = ref<string | null>(null)
const saving = ref(false)

onMounted(async () => {
  if (!props.id) return
  try {
    const { name, email, role, tier, status } = await props.api.load(props.id)
    Object.assign(form, { name, email, role, tier, status })
  } catch {
    problem.value = 'This member could not be loaded.'
  }
})

async function save() {
  saving.value = true
  fieldErrors.value = {}
  problem.value = null
  try {
    await (props.id ? props.api.edit(props.id, { ...form }) : props.api.add({ ...form }))
    await router.push({ name: 'members' })
  } catch (error) {
    if (error instanceof ApiError && error.status === 400) {
      fieldErrors.value = error.fieldErrors
    } else if (error instanceof ApiError && error.detail) {
      problem.value = error.detail
    } else {
      problem.value = 'Saving failed. Try again shortly.'
    }
  } finally {
    saving.value = false
  }
}

const errorFor = (field: string) => fieldErrors.value[field]?.[0]
</script>

<template>
  <section>
    <h1>{{ id ? 'Edit member' : 'Add member' }}</h1>
    <p v-if="problem" role="alert">{{ problem }}</p>
    <form novalidate @submit.prevent="save">
      <label>
        Name
        <input v-model="form.name" name="name" autocomplete="off" required />
        <span v-if="errorFor('name')" class="error" data-field="name">{{ errorFor('name') }}</span>
      </label>
      <label>
        Email
        <input v-model="form.email" name="email" type="email" autocomplete="off" required />
        <span v-if="errorFor('email')" class="error" data-field="email">{{ errorFor('email') }}</span>
      </label>
      <label>
        Role
        <select v-model="form.role" name="role">
          <option v-for="role in roles" :key="role.value" :value="role.value">{{ role.label }}</option>
        </select>
        <span v-if="errorFor('role')" class="error" data-field="role">{{ errorFor('role') }}</span>
      </label>
      <label>
        Tier
        <select v-model="form.tier" name="tier">
          <option :value="null">None</option>
          <option v-for="tier in tiers" :key="tier" :value="tier">{{ tier }}</option>
        </select>
        <span v-if="errorFor('tier')" class="error" data-field="tier">{{ errorFor('tier') }}</span>
      </label>
      <label>
        Status
        <select v-model="form.status" name="status">
          <option v-for="status in statuses" :key="status" :value="status">{{ status }}</option>
        </select>
        <span v-if="errorFor('status')" class="error" data-field="status">{{ errorFor('status') }}</span>
      </label>
      <div class="actions">
        <button type="submit" :disabled="saving">Save</button>
        <RouterLink :to="{ name: 'members' }">Cancel</RouterLink>
      </div>
    </form>
  </section>
</template>

<style scoped>
form {
  display: grid;
  gap: 1rem;
  max-width: 28rem;
}
label {
  display: grid;
  gap: 0.25rem;
}
input,
select {
  font: inherit;
  padding: 0.5rem;
}
.error {
  color: #cf222e;
}
.actions {
  display: flex;
  gap: 1rem;
  align-items: center;
}
</style>
