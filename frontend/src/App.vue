<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import api from './services/api';
import authService from './services/auth';
import type { User, Service, ServiceType, EmployeeSummary, ToastNotification } from './types';
import Navbar from './components/Navbar.vue';
import BottomNav from './components/BottomNav.vue';
import Toast from './components/Toast.vue';
import RegisterServiceModal from './components/RegisterServiceModal.vue';
import ServiceTypeModal from './components/ServiceTypeModal.vue';
import AuthModal from './components/AuthModal.vue';
import UserModal from './components/UserModal.vue';
import FirstLoginPasswordModal from './components/FirstLoginPasswordModal.vue';

const route = useRoute();
const router = useRouter();

// Estado global de la aplicación sincronizado con Vue Router
const currentTab = computed<string>(() => {
  if (route.path.startsWith('/services')) return 'services';
  if (route.path.startsWith('/catalog')) return 'catalog';
  if (route.path.startsWith('/users')) return 'users';
  if (route.path.startsWith('/profile')) return 'profile';
  return 'dashboard';
});

const backendOnline = ref<boolean>(false);
const loading = ref<boolean>(true);
const modalLoading = ref<boolean>(false);

const employees = ref<User[]>([]);
const currentEmployeeId = ref<number | string>(1);
const createDefaultSummary = (u?: Partial<User> | null): EmployeeSummary => ({
  fullName: u?.fullName || 'Chofer Activo',
  code: u?.code || 'CHO-001',
  role: u?.role || 'Conductor',
  department: u?.department || 'Operaciones',
  commissionRate: u?.commissionRate || 25,
  totalEarningsAccumulated: 0,
  totalServicesCompleted: 0,
  totalBilled: 0,
  averageEarningsPerService: 0,
  recentServices: []
});
const employeeSummary = ref<EmployeeSummary>(createDefaultSummary(null));
const services = ref<Service[]>([]);
const serviceTypes = ref<ServiceType[]>([]);
const users = ref<User[]>([]);

// Autenticación y Sesión de Usuario (lectura síncrona de caché inicial)
const getCachedUser = (): User | null => {
  try {
    const cached = localStorage.getItem('auth_user') || sessionStorage.getItem('auth_user') || localStorage.getItem('tito_current_user');
    return cached ? JSON.parse(cached) : null;
  } catch {
    return null;
  }
};
const currentUser = ref<User | null>(getCachedUser());
const showAuthModal = ref<boolean>(false);

// Modales del sistema
const showRegisterModal = ref<boolean>(false);
const showCatalogModal = ref<boolean>(false);
const showUserModal = ref<boolean>(false);
const showFirstLoginModal = ref<boolean>(false);
const editingServiceType = ref<ServiceType | null>(null);
const editingUser = ref<User | null>(null);

// Notificaciones Toast
const toast = ref<ToastNotification>({
  show: false,
  message: '',
  type: 'success'
});

const showToast = (message: string, type: ToastNotification['type'] = 'success') => {
  toast.value = { show: true, message, type };
  setTimeout(() => {
    toast.value.show = false;
  }, 4000);
};

// Determinar si el usuario activo es Administrador
const isAdmin = computed<boolean>(() => {
  return !!(currentUser.value?.isSuperAdmin || 
         currentUser.value?.role === 'Administrador' || 
         currentUser.value?.role === 'SuperAdmin');
});

// Chofer actualmente seleccionado
const currentEmployee = computed<any>(() => {
  return employees.value.find(e => e.id === Number(currentEmployeeId.value)) || {
    id: currentEmployeeId.value,
    fullName: currentUser.value?.fullName || 'Chofer Activo',
    role: currentUser.value?.role || 'Conductor',
    commissionRate: currentUser.value?.commissionRate || 25
  };
});

// Carga inicial y sincronización
const checkHealth = async () => {
  try {
    const status = await api.getStatus();
    backendOnline.value = status?.status === 'Healthy';
  } catch {
    backendOnline.value = false;
  }
};

const loadEmployees = async () => {
  if (!currentUser.value) return;
  try {
    const backendList = await api.getEmployees().catch(() => []);
    employees.value = backendList || [];

    // Si hay un chofer logueado, sincronizar
    if (currentUser.value?.role === 'Chofer') {
      const match = employees.value.find(e => e.email === currentUser.value?.email || e.id === currentUser.value?.id);
      if (match) {
        currentEmployeeId.value = match.id;
      }
    } else if (isAdmin.value && employees.value.length > 0 && !currentEmployeeId.value) {
      currentEmployeeId.value = employees.value[0].id;
    }
  } catch (err) {
    console.warn('No se pudo cargar la lista de empleados:', err);
  }
};

const loadEmployeeData = async () => {
  if (!currentUser.value) return;
  try {
    loading.value = true;
    if (isAdmin.value) {
      const allServices = await api.getAllServices().catch(() => []);
      services.value = allServices || [];
    } else {
      if (!currentEmployeeId.value) return;
      const [summary, srvList] = await Promise.all([
        api.getEmployeeSummary(currentEmployeeId.value).catch(() => null),
        api.getServicesByEmployee(currentEmployeeId.value).catch(() => [])
      ]);
      if (summary) employeeSummary.value = summary;
      services.value = srvList || [];
    }
  } catch (error) {
    console.error('Error al cargar datos del chofer:', error);
  } finally {
    loading.value = false;
  }
};

const loadCatalog = async () => {
  if (!currentUser.value) return;
  try {
    const types = await api.getServiceTypes().catch(() => []);
    serviceTypes.value = types || [];
  } catch (error) {
    console.error('Error al cargar catálogo:', error);
  }
};

const loadUsers = async () => {
  if (!currentUser.value || !isAdmin.value) return;
  try {
    const list = await api.getUsers().catch(() => []);
    users.value = list || [];
  } catch (error) {
    console.error('Error al cargar usuarios:', error);
  }
};

const refreshAll = async () => {
  await checkHealth();
  if (!currentUser.value) return;

  await Promise.all([
    loadEmployees(),
    loadEmployeeData(),
    loadCatalog(),
    loadUsers()
  ]);
};

onMounted(async () => {
  await checkHealth();
  setInterval(checkHealth, 15000);

  const verifiedUser = await authService.checkAuth();
  if (verifiedUser) {
    currentUser.value = verifiedUser;
    if (verifiedUser.role === 'Chofer') {
      currentEmployeeId.value = verifiedUser.id;
    }
    if (verifiedUser.mustChangePassword) {
      showFirstLoginModal.value = true;
    }
    await refreshAll();
  } else {
    // Si no hay nadie autenticado, limpiar estados y abrir modal de login
    currentUser.value = null;
    currentEmployeeId.value = '';
    services.value = [];
    employeeSummary.value = createDefaultSummary(null);
    showAuthModal.value = true;
  }
});

// Manejo de Autenticación
const onLoginSuccess = async (session: any) => {
  const user = session.user || session;
  currentUser.value = user;
  showAuthModal.value = false;

  if (user.role === 'Chofer') {
    currentEmployeeId.value = user.id;
    employeeSummary.value = createDefaultSummary(user);
  }

  if (user.mustChangePassword) {
    showFirstLoginModal.value = true;
  }

  showToast(`¡Bienvenido de vuelta, ${user.fullName}! (${user.role})`, 'success');
  router.push('/dashboard').catch(() => {});

  await refreshAll();
};

const onFirstPasswordChanged = () => {
  showFirstLoginModal.value = false;
  if (currentUser.value) {
    currentUser.value.mustChangePassword = false;
  }
  showToast('¡Contraseña personalizada configurada con éxito! Bienvenido al sistema.', 'success');
};

const onLogout = async () => {
  await authService.logout();
  currentUser.value = null;
  currentEmployeeId.value = '';
  services.value = [];
  employeeSummary.value = createDefaultSummary(null);
  showFirstLoginModal.value = false;
  router.push('/dashboard').catch(() => {});
  showToast('Has cerrado sesión correctamente.', 'info');
  showAuthModal.value = true;
};

// Control de navegación con Vue Router
const onTabChange = (newTab: string) => {
  const pathMap: Record<string, string> = {
    dashboard: '/dashboard',
    services: '/services',
    catalog: '/catalog',
    settlements: '/settlements',
    users: '/users',
    profile: '/profile'
  };

  if (!isAdmin.value && (newTab === 'catalog' || newTab === 'users' || newTab === 'settlements')) {
    router.push('/dashboard');
    return;
  }

  const targetPath = pathMap[newTab] || '/dashboard';
  router.push(targetPath);
};

// Actualización de datos de perfil
const onUpdateProfile = (updatedUser: Partial<User>) => {
  currentUser.value = { ...currentUser.value!, ...updatedUser };
  const idx = employees.value.findIndex(e => e.id === updatedUser.id);
  if (idx !== -1) {
    employees.value[idx] = { ...employees.value[idx], ...updatedUser } as User;
  }
};

// Manejo de Chofer
const onEmployeeChange = async (newId: number | string) => {
  currentEmployeeId.value = newId;
  await loadEmployeeData();
  showToast(`Cambiado a chofer: ${currentEmployee.value.fullName}`, 'info');
};

// Manejo de Registro de Viajes
const onRegisterService = async (payload: any) => {
  try {
    modalLoading.value = true;
    const created = await api.registerService(payload);
    showToast(`¡Viaje ${created.serviceCode} registrado!`, 'success');
    showRegisterModal.value = false;
    await loadEmployeeData();
  } catch (error: any) {
    showToast(error.message || 'Error al registrar el viaje', 'error');
  } finally {
    modalLoading.value = false;
  }
};

// Manejo del Catálogo (CRUD)
const openCreateCatalogModal = () => {
  editingServiceType.value = null;
  showCatalogModal.value = true;
};

const openEditCatalogModal = (st: ServiceType) => {
  editingServiceType.value = st;
  showCatalogModal.value = true;
};

const onSaveServiceType = async (payload: Partial<ServiceType>) => {
  try {
    modalLoading.value = true;
    if (editingServiceType.value) {
      await api.updateServiceType(editingServiceType.value.id, payload);
      showToast('Tipo de servicio actualizado correctamente', 'success');
    } else {
      await api.createServiceType(payload);
      showToast('Nuevo servicio agregado al catálogo', 'success');
    }
    showCatalogModal.value = false;
    await loadCatalog();
  } catch (error: any) {
    showToast(error.message || 'Error al guardar en el catálogo', 'error');
  } finally {
    modalLoading.value = false;
  }
};

const onDeleteServiceType = async (id: number | string) => {
  try {
    await api.softDeleteServiceType(id);
    showToast('Servicio eliminado lógicamente (Soft Delete) del catálogo', 'success');
    await loadCatalog();
  } catch (error: any) {
    showToast(error.message || 'Error al eliminar el servicio', 'error');
  }
};

// Manejo de Gestión de Personal / Usuarios (CRUD)
const openCreateUserModal = () => {
  editingUser.value = null;
  showUserModal.value = true;
};

const openEditUserModal = (u: User) => {
  editingUser.value = u;
  showUserModal.value = true;
};

const onSaveUser = async (payload: Partial<User>) => {
  try {
    modalLoading.value = true;
    if (editingUser.value) {
      await api.updateUser(editingUser.value.id, payload);
      showToast(`Usuario ${payload.fullName} actualizado correctamente`, 'success');
    } else {
      const created = await api.createUser(payload);
      showToast(`¡Usuario ${created.fullName} (${created.code}) creado exitosamente! Clave temporal de acceso: ${created.code}`, 'success');
    }
    showUserModal.value = false;
    await Promise.all([
      loadUsers(),
      loadEmployees()
    ]);
  } catch (error: any) {
    showToast(error.message || 'Error al guardar el usuario', 'error');
  } finally {
    modalLoading.value = false;
  }
};

const onDeleteUser = async (id: number | string) => {
  try {
    await api.deleteUser(id);
    showToast('Usuario dado de baja (Soft delete) correctamente', 'success');
    await Promise.all([
      loadUsers(),
      loadEmployees()
    ]);
  } catch (error: any) {
    showToast(error.message || 'Error al dar de baja el usuario', 'error');
  }
};
</script>

<template>
  <div class="min-h-screen flex flex-col bg-brand-black">
    <!-- Barra Superior -->
    <Navbar 
      v-if="currentUser"
      :employees="employees"
      :currentEmployeeId="currentEmployeeId"
      :backendOnline="backendOnline"
      :currentTab="currentTab"
      :currentUser="currentUser"
      @change-employee="onEmployeeChange"
      @change-tab="onTabChange"
      @open-new-service="showRegisterModal = true"
      @logout="onLogout"
    />

    <!-- Contenido Principal Dinámico con Vue Router -->
    <main v-if="currentUser" class="flex-1 px-4 py-6 pb-24 md:p-8">
      <div class="max-w-[1300px] mx-auto">
        <RouterView v-slot="{ Component }">
          <component 
            :is="Component"
            :employeeSummary="employeeSummary"
            :services="services"
            :loading="loading"
            :currentUser="currentUser"
            :drivers="employees"
            :serviceTypes="serviceTypes"
            :users="users"
            @open-new-service="showRegisterModal = true"
            @refresh="loadEmployeeData"
            @open-create-modal="openCreateCatalogModal"
            @edit-service-type="openEditCatalogModal"
            @delete-service-type="onDeleteServiceType"
            @open-create-user-modal="openCreateUserModal"
            @edit-user="openEditUserModal"
            @delete-user="onDeleteUser"
            @update-profile="onUpdateProfile"
            @logout="onLogout"
            @notify="showToast"
          />
        </RouterView>
      </div>
    </main>

    <!-- Barra de Navegación Táctil Inferior (Móvil) -->
    <BottomNav 
      v-if="currentUser"
      :currentTab="currentTab"
      :currentUser="currentUser"
      @change-tab="onTabChange"
      @open-new-service="showRegisterModal = true"
      @refresh="refreshAll"
    />

    <!-- Modal de Inicio de Sesión (Activo cuando NO hay usuario autenticado) -->
    <AuthModal 
      :show="!currentUser"
      :canClose="false"
      @login-success="onLoginSuccess"
    />

    <!-- Modal Personal / Usuario (Crear / Editar) -->
    <UserModal 
      :show="showUserModal"
      :user="editingUser"
      :loading="modalLoading"
      @close="showUserModal = false"
      @submit="onSaveUser"
    />

    <!-- Modal Cambio Obligatorio de Contraseña Primer Inicio -->
    <FirstLoginPasswordModal 
      :show="showFirstLoginModal"
      :currentUser="currentUser"
      @password-changed="onFirstPasswordChanged"
      @logout="onLogout"
    />

    <!-- Modal Registrar Viaje / Servicio -->
    <RegisterServiceModal 
      :show="showRegisterModal"
      :employee="currentEmployee"
      :drivers="employees"
      :isAdmin="isAdmin"
      :serviceTypes="serviceTypes"
      :loading="modalLoading"
      @close="showRegisterModal = false"
      @submit="onRegisterService"
    />

    <!-- Modal Catálogo (Crear / Editar) -->
    <ServiceTypeModal 
      :show="showCatalogModal"
      :serviceType="editingServiceType"
      :loading="modalLoading"
      @close="showCatalogModal = false"
      @submit="onSaveServiceType"
    />

    <!-- Notificaciones Toast -->
    <Toast 
      :show="toast.show"
      :message="toast.message"
      :type="toast.type"
      @close="toast.show = false"
    />
  </div>
</template>
