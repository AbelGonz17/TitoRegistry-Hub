<script setup>
import { ref, onMounted, computed } from 'vue';
import api from './services/api';
import authService from './services/auth';
import Navbar from './components/Navbar.vue';
import BottomNav from './components/BottomNav.vue';
import Toast from './components/Toast.vue';
import RegisterServiceModal from './components/RegisterServiceModal.vue';
import ServiceTypeModal from './components/ServiceTypeModal.vue';
import AuthModal from './components/AuthModal.vue';
import UserModal from './components/UserModal.vue';
import DashboardView from './views/DashboardView.vue';
import CatalogView from './views/CatalogView.vue';
import UsersView from './views/UsersView.vue';

// Estado global de la aplicación
const currentTab = ref('dashboard'); // 'dashboard' | 'catalog' | 'users'
const backendOnline = ref(false);
const loading = ref(true);
const modalLoading = ref(false);

const employees = ref([]);
const currentEmployeeId = ref(1);
const employeeSummary = ref({});
const services = ref([]);
const allServices = ref([]);
const driverSummaries = ref([]);
const serviceTypes = ref([]);
const users = ref([]);

// Autenticación y Sesión de Usuario (lectura síncrona de caché inicial protegida)
const getCachedUser = () => {
  try {
    const raw = 
      localStorage.getItem('tito_auth_user') || 
      sessionStorage.getItem('tito_auth_user') || 
      localStorage.getItem('auth_user') || 
      localStorage.getItem('tito_current_user');

    if (!raw) return null;
    const user = JSON.parse(raw);

    // Si viene de otra aplicación ajena (ej. gym.com o no tiene nombre/email válido de Tito), purgarlo
    if (!user || (!user.fullName && !user.name) || user.email?.includes('@gym.com')) {
      authService.logout();
      return null;
    }

    // Normalizar propiedad fullName y avatar
    if (!user.fullName && user.name) user.fullName = user.name;
    if (!user.avatar) {
      user.avatar = (user.role === 'SuperAdmin' || user.isSuperAdmin) ? '👑' : (user.role === 'Chofer' ? '🚚' : '👔');
    }
    return user;
  } catch {
    return null;
  }
};
const currentUser = ref(getCachedUser());
const showAuthModal = ref(false);

// Modales del sistema
const showRegisterModal = ref(false);
const showCatalogModal = ref(false);
const showUserModal = ref(false);
const editingServiceType = ref(null);
const editingUser = ref(null);

// Notificaciones Toast
const toast = ref({
  show: false,
  message: '',
  type: 'success'
});

const showToast = (message, type = 'success') => {
  toast.value = { show: true, message, type };
  setTimeout(() => {
    toast.value.show = false;
  }, 4000);
};

// Chofer actualmente seleccionado
const currentEmployee = computed(() => {
  return employees.value.find(e => e.id === currentEmployeeId.value) || {
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
  } catch (error) {
    backendOnline.value = false;
  }
};

const loadEmployees = async () => {
  try {
    const backendList = await api.getEmployees().catch(() => []);
    employees.value = backendList || [];

    // Si hay un chofer logueado, sincronizar
    if (currentUser.value?.role === 'Chofer') {
      const match = employees.value.find(e => e.email === currentUser.value.email || e.id === currentUser.value.id);
      if (match) {
        currentEmployeeId.value = match.id;
      }
    } else if (employees.value.length > 0 && !currentEmployeeId.value) {
      currentEmployeeId.value = employees.value[0].id;
    }
  } catch (err) {
    console.warn('No se pudo cargar la lista de empleados:', err);
  }
};

const loadEmployeeData = async () => {
  if (!currentEmployeeId.value) return;
  try {
    loading.value = true;
    const [summary, srvList] = await Promise.all([
      api.getEmployeeSummary(currentEmployeeId.value).catch(() => null),
      api.getServicesByEmployee(currentEmployeeId.value).catch(() => [])
    ]);

    if (summary) {
      employeeSummary.value = summary;
    } else {
      employeeSummary.value = {
        id: currentEmployee.value.id,
        code: currentEmployee.value.code || 'CHO-001',
        fullName: currentEmployee.value.fullName,
        email: currentEmployee.value.email || '',
        phone: currentEmployee.value.phone || '',
        role: currentEmployee.value.role || 'Conductor',
        department: currentEmployee.value.department || 'Operaciones',
        commissionRate: currentEmployee.value.commissionRate || 25,
        isActive: true,
        totalServicesCompleted: 0,
        totalEarningsAccumulated: 0,
        totalBilled: 0,
        averageEarningsPerService: 0,
        recentServices: []
      };
    }
    services.value = srvList || [];
  } catch (err) {
    showToast('Error al cargar datos del chofer', 'error');
  } finally {
    loading.value = false;
  }
};

const loadCatalog = async () => {
  try {
    const list = await api.getServiceTypes();
    serviceTypes.value = list || [];
  } catch (err) {
    console.warn('Error al cargar catálogo:', err);
  }
};

const loadUsers = async () => {
  try {
    const list = await api.getUsers();
    users.value = list || [];
  } catch (err) {
    console.warn('Error al cargar usuarios:', err);
  }
};

const loadFleetData = async () => {
  if (currentUser.value?.role !== 'Chofer') {
    try {
      const [allSrv, usersList] = await Promise.all([
        api.getAllServices().catch(() => []),
        api.getUsers().catch(() => [])
      ]);
      allServices.value = allSrv || [];
      if (usersList && usersList.length > 0) {
        users.value = usersList;
      }

      const drivers = (users.value || []).filter(u => u.role === 'Chofer');
      if (drivers.length > 0) {
        const summaries = await Promise.all(
          drivers.map(d => api.getEmployeeSummary(d.id).catch(() => null))
        );
        driverSummaries.value = summaries.filter(Boolean);
      }
    } catch (e) {
      console.warn('Error al cargar datos globales de la flota:', e);
    }
  }
};

const refreshAll = async () => {
  await checkHealth();
  await Promise.all([
    loadEmployees(),
    loadEmployeeData(),
    loadCatalog(),
    loadUsers(),
    loadFleetData()
  ]);
};

onMounted(async () => {
  // Verificar token en el backend
  const verifiedUser = await authService.checkAuth();
  if (verifiedUser) {
    currentUser.value = verifiedUser;
  } else {
    currentUser.value = null;
  }

  await refreshAll();
  setInterval(checkHealth, 15000);

  // Si no hay sesión iniciada, abrir ventana de login
  if (!currentUser.value) {
    showAuthModal.value = true;
  } else if (currentUser.value.role === 'Chofer') {
    const match = employees.value.find(e => e.email === currentUser.value.email || e.id === currentUser.value.id);
    if (match) currentEmployeeId.value = match.id;
    if (currentTab.value === 'users') currentTab.value = 'dashboard';
  }
});

// Manejo de Navegación con Guardas de Rol
const onChangeTab = (tab) => {
  if (tab === 'users' && currentUser.value?.role === 'Chofer') {
    showToast('La gestión de personal y nómina está reservada para Administradores.', 'error');
    currentTab.value = 'dashboard';
    return;
  }
  currentTab.value = tab;
};

// Manejo de Autenticación
const openAuthModal = () => {
  showAuthModal.value = true;
};

const onLoginSuccess = async (session) => {
  currentUser.value = session;
  showAuthModal.value = false;

  await refreshAll();

  // Si es chofer, enfocar su ID y restringir tab a dashboard
  if (session.role === 'Chofer') {
    const match = employees.value.find(e => e.email === session.email || e.id === session.id);
    if (match) currentEmployeeId.value = match.id;
    if (currentTab.value === 'users') currentTab.value = 'dashboard';
  }

  showToast(`¡Bienvenido de vuelta, ${session.fullName}! (${session.role})`, 'success');
};

const onLogout = () => {
  authService.logout();
  currentUser.value = null;
  currentTab.value = 'dashboard';
  showToast('Has cerrado sesión correctamente.', 'info');
  showAuthModal.value = true;
};

// Manejo de Chofer
const onEmployeeChange = async (newId) => {
  if (currentUser.value?.role === 'Chofer') {
    return; // Chofer no puede visualizar ganancias de otros choferes
  }
  currentEmployeeId.value = newId;
  await loadEmployeeData();
  showToast(`Cambiado a chofer: ${currentEmployee.value.fullName}`, 'info');
};

// Manejo de Registro de Viajes
const onRegisterService = async (payload) => {
  try {
    modalLoading.value = true;
    const created = await api.registerService(payload);
    showToast(`¡Viaje ${created.serviceCode} registrado! Ganancia: +$${created.employeeEarnings.toFixed(2)}`, 'success');
    showRegisterModal.value = false;
    await loadEmployeeData();
  } catch (error) {
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

const openEditCatalogModal = (st) => {
  editingServiceType.value = st;
  showCatalogModal.value = true;
};

const onSaveServiceType = async (payload) => {
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
  } catch (error) {
    showToast(error.message || 'Error al guardar en el catálogo', 'error');
  } finally {
    modalLoading.value = false;
  }
};

const onDeleteServiceType = async (id) => {
  try {
    await api.softDeleteServiceType(id);
    showToast('Servicio eliminado lógicamente (Soft Delete) del catálogo', 'success');
    await loadCatalog();
  } catch (error) {
    showToast(error.message || 'Error al eliminar el servicio', 'error');
  }
};

// Manejo de Gestión de Personal / Usuarios (CRUD)
const openCreateUserModal = () => {
  editingUser.value = null;
  showUserModal.value = true;
};

const openEditUserModal = (u) => {
  editingUser.value = u;
  showUserModal.value = true;
};

const onSaveUser = async (payload) => {
  try {
    modalLoading.value = true;
    if (editingUser.value) {
      await api.updateUser(editingUser.value.id, payload);
      showToast(`Usuario ${payload.fullName} actualizado correctamente`, 'success');
    } else {
      const created = await api.createUser(payload);
      showToast(`¡Usuario ${created.fullName} (${created.code}) creado exitosamente!`, 'success');
    }
    showUserModal.value = false;
    await Promise.all([
      loadUsers(),
      loadEmployees()
    ]);
  } catch (error) {
    showToast(error.message || 'Error al guardar el usuario', 'error');
  } finally {
    modalLoading.value = false;
  }
};

const onDeleteUser = async (id) => {
  try {
    await api.deleteUser(id);
    showToast('Usuario dado de baja (Soft delete) correctamente', 'success');
    await Promise.all([
      loadUsers(),
      loadEmployees()
    ]);
  } catch (error) {
    showToast(error.message || 'Error al dar de baja el usuario', 'error');
  }
};
</script>

<template>
  <div class="app-layout">
    <!-- Barra Superior -->
    <Navbar 
      :employees="employees"
      :currentEmployeeId="currentEmployeeId"
      :backendOnline="backendOnline"
      :currentTab="currentTab"
      :currentUser="currentUser"
      @change-employee="onEmployeeChange"
      @change-tab="onChangeTab"
      @open-new-service="showRegisterModal = true"
      @open-auth="openAuthModal"
      @logout="onLogout"
    />

    <!-- Contenido Principal -->
    <main class="main-content">
      <div class="content-container">
        <!-- Vista 1: Panel de Control de Viajes y Ganancias -->
        <DashboardView 
          v-if="currentTab === 'dashboard'"
          :employeeSummary="employeeSummary"
          :services="services"
          :allServices="allServices"
          :employees="employees"
          :users="users"
          :serviceTypes="serviceTypes"
          :driverSummaries="driverSummaries"
          :currentUser="currentUser"
          :backendOnline="backendOnline"
          :loading="loading"
          @open-new-service="showRegisterModal = true"
          @change-employee="onEmployeeChange"
          @change-tab="onChangeTab"
          @refresh="refreshAll"
        />

        <!-- Vista 2: Catálogo Dinámico de Rutas y Servicios -->
        <CatalogView 
          v-else-if="currentTab === 'catalog'"
          :serviceTypes="serviceTypes"
          :loading="loading"
          @open-create-modal="openCreateCatalogModal"
          @edit-service-type="openEditCatalogModal"
          @delete-service-type="onDeleteServiceType"
        />

        <!-- Vista 3: Gestión de Personal & Usuarios (Choferes & Administradores) -->
        <UsersView 
          v-else-if="currentTab === 'users'"
          :users="users"
          :loading="loading"
          :currentUser="currentUser"
          @open-create-modal="openCreateUserModal"
          @edit-user="openEditUserModal"
          @delete-user="onDeleteUser"
          @refresh="loadUsers"
        />
      </div>
    </main>

    <!-- Barra de Navegación Táctil Inferior (Móvil) -->
    <BottomNav 
      :currentTab="currentTab"
      :currentUser="currentUser"
      @change-tab="onChangeTab"
      @open-new-service="showRegisterModal = true"
      @open-auth="openAuthModal"
      @logout="onLogout"
      @refresh="refreshAll"
    />

    <!-- Modal de Inicio de Sesión -->
    <AuthModal 
      :show="showAuthModal"
      :canClose="true"
      @close="showAuthModal = false"
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

    <!-- Modal Registrar Viaje / Servicio -->
    <RegisterServiceModal 
      :show="showRegisterModal"
      :employee="currentEmployee"
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

<style scoped>
.app-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--brand-black);
}

.main-content {
  flex: 1;
  padding: 24px 18px 80px; /* Espacio inferior para el BottomNav móvil */
}

@media (min-width: 769px) {
  .main-content {
    padding: 32px 24px;
  }
}

.content-container {
  max-width: 1300px;
  margin: 0 auto;
}
</style>
