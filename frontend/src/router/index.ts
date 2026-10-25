import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import DashboardView from '../views/DashboardView.vue';
import DriverServicesView from '../views/DriverServicesView.vue';
import CatalogView from '../views/CatalogView.vue';
import UsersView from '../views/UsersView.vue';
import SettlementsView from '../views/SettlementsView.vue';
import ProfileView from '../views/ProfileView.vue';
import { getToken } from '../services/auth';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/dashboard'
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: DashboardView,
    meta: {
      title: 'Panel',
      tabKey: 'dashboard',
      icon: '📊'
    }
  },
  {
    path: '/services',
    name: 'services',
    component: DriverServicesView,
    meta: {
      title: 'Mis Servicios',
      tabKey: 'services',
      icon: '🚐',
      driverOnly: true
    }
  },
  {
    path: '/catalog',
    name: 'catalog',
    component: CatalogView,
    meta: {
      title: 'Catálogo Rutas',
      tabKey: 'catalog',
      icon: '📋',
      adminOnly: true
    }
  },
  {
    path: '/settlements',
    name: 'settlements',
    component: SettlementsView,
    meta: {
      title: 'Liquidaciones & Planilla',
      tabKey: 'settlements',
      icon: '📄',
      adminOnly: true
    }
  },

  {
    path: '/users',
    name: 'users',
    component: UsersView,
    meta: {
      title: 'Personal & Choferes',
      tabKey: 'users',
      icon: '👥',
      adminOnly: true
    }
  },
  {
    path: '/profile',
    name: 'profile',
    component: ProfileView,
    meta: {
      title: 'Mi Perfil',
      tabKey: 'profile',
      icon: '👤'
    }
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/dashboard'
  }
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 };
  }
});

// Guardias de navegación para protección por roles y autenticación
router.beforeEach((to, _from, next) => {
  const token = getToken();
  let user: any = null;
  try {
    const raw = localStorage.getItem('auth_user') || sessionStorage.getItem('auth_user');
    if (raw) user = JSON.parse(raw);
  } catch {
    user = null;
  }

  const isAdmin = user?.isSuperAdmin || user?.role === 'Administrador' || user?.role === 'SuperAdmin';

  // Si intenta acceder a rutas exclusivas de admin siendo chofer
  if (to.meta.adminOnly && !isAdmin && token) {
    next('/dashboard');
    return;
  }

  // Si un admin intenta acceder a la pestaña exclusiva de chofer
  if (to.meta.driverOnly && isAdmin && token) {
    next('/dashboard');
    return;
  }

  next();
});

export default router;
