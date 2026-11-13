import { createApp } from 'vue'
import App from './App.vue'
import { getMe } from './api/client'
import { createAppRouter } from './router'

createApp(App).use(createAppRouter({ getMe })).mount('#app')
