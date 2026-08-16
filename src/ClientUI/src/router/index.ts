import type { RouteLocationNormalized } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import { createRouter, createWebHistory } from 'vue-router'

import AppLayout from '../layouts/AppLayout.vue'
import LoginView from '../views/LoginView.vue'
import HomeView from '../views/HomeView.vue'
import DashboardView from '../views/DashboardView.vue'

const routes: RouteRecordRaw[] = [
    {
        path: '/',
        component: AppLayout,
        meta: { requiresAuth: true },
        children: [
            {
                path: 'login',
                name: 'login',
                component: LoginView,
                meta: {
                    anonymous: true,
                },
            },
            {
                path: '',
                name: 'home',
                component: HomeView,
                meta: {
                    anonymous: true,
                },
            },
            {
                path: 'dashboard',
                name: 'dashboard',
                component: DashboardView,
                meta: {},
            },
        ],
    },
]

const router = createRouter({
    history: createWebHistory(),
    routes,
})

router.beforeEach(async (to, _from, next) => {
    // Check if target route requires authentication
    if (to.meta.anonymous) {
        next()
        return
    }

    const { isAuthenticated, checkAuth } = useAuth()
    const loggedIn = isAuthenticated.value || (await checkAuth())

    if (!loggedIn) {
        return next({ name: 'Login', query: { redirect: to.fullPath } })
    }

    next()
})

export default router
