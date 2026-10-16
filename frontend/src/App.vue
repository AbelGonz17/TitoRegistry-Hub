<script setup>
import { ref, onMounted, computed } from 'vue';
import api from './services/api';
import Navbar from './components/Navbar.vue';
import BottomNav from './components/BottomNav.vue';
import Toast from './components/Toast.vue';
import RegisterServiceModal from './components/RegisterServiceModal.vue';
import ServiceTypeModal from './components/ServiceTypeModal.vue';
import DashboardView from './views/DashboardView.vue';
import CatalogView from './views/CatalogView.vue';

// Estado global de la aplicación
const currentTab = ref('dashboard'); // 'dashboard' | 'catalog'
const backendOnline = ref(false);
const loading = ref(true);
const modalLoading = ref(false);

const employees = ref([]);
const currentEmployeeId = ref(1);
const employeeSummary = ref({});
const services = ref([]);
const serviceTypes = ref([]);

// Modales
const showRegisterModal = ref(false);
const showCatalogModal = ref(false);
const editingServiceType = ref(null);

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
    fullName: 'Chofer',
    commissionRate: 25
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
    const list = await api.getEmployees();
    employees.value = list || [];
    if (employees.value.length > 0 && !currentEmployeeId.value) {
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

const refreshAll = async () => {
  await checkHealth();
  await Promise.all([
    loadEmployees(),
    loadEmployeeData(),
    loadCatalog()
  ]);
};

onMounted(async () => {
  await refreshAll();
  // Auto-refresco del estado cada 15 segundos
  setInterval(checkHealth, 15000);
});

// Manejo de Chofer
const onEmployeeChange = async (newId) => {
  currentEmployeeId.value = newId;
  await loadEmployeeData();
  showToast(`Cambiado a chofer: ${currentEmployee.value.fullName}`, 'info');
};

// Manejo de Registro de Viajes
const onRegisterService = async (payload) => {
  try {
    modalLoading.value = true;
    const created = await api.registerService(payload);
    showToast(`¡Viaje ${created.serviceCode} registrado! Ganancia sumada: +$${created.employeeEarnings.toFixed(2)}`, 'success');
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
</script>

<template>
  <div class="app-layout">
    <!-- Barra Superior -->
    <Navbar 
      :employees="employees"
      :currentEmployeeId="currentEmployeeId"
      :backendOnline="backendOnline"
      :currentTab="currentTab"
      @change-employee="onEmployeeChange"
      @change-tab="tab => currentTab = tab"
      @open-new-service="showRegisterModal = true"
    />

    <!-- Contenido Principal -->
    <main class="main-content">
      <div class="content-container">
        <!-- Vistas -->
        <DashboardView 
          v-if="currentTab === 'dashboard'"
          :employeeSummary="employeeSummary"
          :services="services"
          :loading="loading"
          @open-new-service="showRegisterModal = true"
          @refresh="loadEmployeeData"
        />

        <CatalogView 
          v-else-if="currentTab === 'catalog'"
          :serviceTypes="serviceTypes"
          :loading="loading"
          @open-create-modal="openCreateCatalogModal"
          @edit-service-type="openEditCatalogModal"
          @delete-service-type="onDeleteServiceType"
        />
      </div>
    </main>

    <!-- Barra de Navegación Táctil Inferior (Móvil) -->
    <BottomNav 
      :currentTab="currentTab"
      @change-tab="tab => currentTab = tab"
      @open-new-service="showRegisterModal = true"
      @refresh="refreshAll"
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
