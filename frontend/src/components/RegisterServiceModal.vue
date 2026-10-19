<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { ServiceType, User } from '../types';

const props = defineProps<{
  show: boolean;
  employee?: { id?: number | string; fullName?: string; commissionRate?: number; role?: string };
  drivers?: User[];
  isAdmin?: boolean;
  serviceTypes?: ServiceType[];
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'submit', payload: any): void;
}>();

// Formulario reactivo alineado con el registro oficial
const formData = ref({
  driverId: '' as string | number,
  clientReference: '',
  serviceTypeId: '' as string | number,
  description: '',
  serviceDate: new Date().toISOString().slice(0, 10),
  priceCharged: '' as string | number,
  notes: ''
});

const errorMessage = ref('');

// Al abrir el modal, inicializar datos
watch(() => props.show, (newVal) => {
  if (newVal) {
    errorMessage.value = '';
    const initialDriverId = props.employee?.id || (props.drivers && props.drivers.length > 0 ? props.drivers[0].id : '');
    const firstServiceType = props.serviceTypes && props.serviceTypes.length > 0 ? props.serviceTypes[0] : null;

    formData.value = {
      driverId: initialDriverId,
      clientReference: '',
      serviceTypeId: firstServiceType ? firstServiceType.id : '',
      description: '',
      serviceDate: new Date().toISOString().slice(0, 10),
      priceCharged: firstServiceType ? (firstServiceType as any).defaultPrice || (firstServiceType as any).basePrice || '' : '',
      notes: ''
    };
  }
});

// Chofer actualmente asociado al cálculo
const activeDriver = computed(() => {
  if (props.isAdmin && formData.value.driverId && props.drivers?.length) {
    return props.drivers.find(d => d.id === Number(formData.value.driverId)) || props.employee;
  }
  return props.employee;
});

// Cuando cambia el tipo de servicio seleccionado, sugerir el precio base del catálogo
const onServiceTypeChange = (event: Event) => {
  const target = event.target as HTMLSelectElement;
  const typeId = Number(target.value);
  formData.value.serviceTypeId = typeId;
  const selected = props.serviceTypes?.find(s => s.id === typeId);
  const basePrice = (selected as any)?.defaultPrice ?? (selected as any)?.basePrice;
  if (selected && (!formData.value.priceCharged || Number(formData.value.priceCharged) === 0)) {
    formData.value.priceCharged = basePrice || '';
  }
};

// Cálculo en vivo de la comisión para el chofer
const estimatedEarnings = computed(() => {
  const price = parseFloat(String(formData.value.priceCharged)) || 0;
  const rate = activeDriver.value?.commissionRate || 25;
  const val = (price * rate) / 100;
  return val.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
});

const handleSubmit = () => {
  errorMessage.value = '';

  const driverId = props.isAdmin 
    ? (Number(formData.value.driverId) || props.employee?.id)
    : props.employee?.id;

  if (!driverId) {
    errorMessage.value = 'Por favor selecciona el chofer responsable.';
    return;
  }
  if (!formData.value.clientReference.trim()) {
    errorMessage.value = 'La Referencia del Cliente es obligatoria.';
    return;
  }
  if (!formData.value.serviceTypeId) {
    errorMessage.value = 'Por favor selecciona el Tipo de Servicio.';
    return;
  }
  if (!formData.value.description.trim()) {
    errorMessage.value = 'Por favor describe el servicio realizado.';
    return;
  }
  const price = parseFloat(String(formData.value.priceCharged));
  if (isNaN(price) || price <= 0) {
    errorMessage.value = 'El Precio del Servicio debe ser mayor a 0.';
    return;
  }

  const payload = {
    driverId: Number(driverId),
    employeeId: Number(driverId),
    serviceTypeId: Number(formData.value.serviceTypeId),
    clientReference: formData.value.clientReference.trim(),
    clientName: formData.value.clientReference.trim(),
    description: formData.value.description.trim(),
    serviceDate: new Date(formData.value.serviceDate).toISOString(),
    priceCharged: price,
    notes: formData.value.notes.trim()
  };

  emit('submit', payload);
};
</script>

<template>
  <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <!-- Encabezado del Modal -->
      <div class="modal-header">
        <div class="header-titles">
          <div class="badge-tag">🚐 Transporte Tito</div>
          <h2 class="modal-title">Registrar Servicio</h2>
          <p class="modal-subtitle">
            Formulario oficial de registro de servicios y viajes
          </p>
        </div>
        <button class="btn-close" @click="$emit('close')" aria-label="Cerrar">&times;</button>
      </div>

      <!-- Formulario -->
      <form @submit.prevent="handleSubmit" class="modal-body">
        <div v-if="errorMessage" class="alert-error">
          <span>⚠️</span>
          <span>{{ errorMessage }}</span>
        </div>

        <!-- 1. Chofer / Conductor (* Elegir) -->
        <div class="form-group" v-if="isAdmin && drivers && drivers.length > 0">
          <label class="form-label">Chofer Asignado *</label>
          <select 
            v-model="formData.driverId" 
            class="form-control"
            required
          >
            <option value="" disabled>Elegir Chofer...</option>
            <option v-for="d in drivers" :key="d.id" :value="d.id">
              {{ d.fullName }} ({{ d.code || 'ID: ' + d.id }}) — Comisión: {{ d.commissionRate || 25 }}%
            </option>
          </select>
        </div>
        <div v-else class="driver-fixed-pill">
          <span class="label">Chofer:</span>
          <span class="name">{{ activeDriver?.fullName || employee?.fullName || 'Chofer Activo' }}</span>
          <span class="rate-badge">{{ activeDriver?.commissionRate || 25 }}% Comisión</span>
        </div>

        <!-- 2. ReferenciaCLIENTE * -->
        <div class="form-group">
          <label class="form-label">ReferenciaCLIENTE *</label>
          <input 
            v-model="formData.clientReference"
            type="text" 
            class="form-control" 
            placeholder="Nombre del cliente, empresa o número de referencia" 
            required
          />
        </div>

        <!-- 3. TIPO DE SERVICIO * -->
        <div class="form-group">
          <label class="form-label">Tipo de Servicio *</label>
          <select 
            class="form-control" 
            :value="formData.serviceTypeId" 
            @change="onServiceTypeChange"
            required
          >
            <option value="" disabled>Elegir tipo de servicio...</option>
            <option v-for="st in serviceTypes" :key="st.id" :value="st.id">
              {{ st.name }} ({{ st.category }}) — Base: RD$ {{ ((st as any).defaultPrice || (st as any).basePrice || 0).toLocaleString('es-DO') }}
            </option>
          </select>
          <p v-if="!serviceTypes || serviceTypes.length === 0" class="field-hint">
            * No hay rutas activas en el catálogo.
          </p>
        </div>

        <!-- 4. DESCRIBA EL SERVICIO REALIZADO * -->
        <div class="form-group">
          <label class="form-label">Describa el Servicio Realizado *</label>
          <textarea 
            v-model="formData.description"
            class="form-control" 
            rows="2" 
            placeholder="Indica el origen, destino, paradas o detalle del servicio completado..."
            required
          ></textarea>
        </div>

        <!-- 5 y 6. FECHA DEL SERVICIO * y PRECIO DEL SERVICIO * -->
        <div class="form-row">
          <div class="form-group flex-1">
            <label class="form-label">Fecha del Servicio *</label>
            <input 
              v-model="formData.serviceDate"
              type="date" 
              class="form-control" 
              required
            />
          </div>
          <div class="form-group flex-1">
            <label class="form-label">Precio del Servicio (RD$) *</label>
            <div class="price-input-wrapper">
              <span class="currency-prefix">RD$</span>
              <input 
                v-model="formData.priceCharged"
                type="number" 
                step="0.01" 
                min="0.01" 
                class="form-control price-input with-prefix" 
                placeholder="0.00" 
                required
              />
            </div>
          </div>
        </div>

        <!-- Caja de Cálculo en Vivo de Comisión para el Chofer -->
        <div class="earnings-preview-card">
          <div class="preview-header">
            <span class="preview-badge">Comisión en Tiempo Real</span>
            <span class="rate-tag">{{ activeDriver?.commissionRate || 25 }}% Chofer</span>
          </div>
          <div class="preview-body">
            <div class="preview-amount">
              <span class="currency">RD$</span>
              <span class="val">{{ estimatedEarnings }}</span>
            </div>
            <p class="preview-desc">
              Esta es la ganancia neta directa que se sumará al historial de comisiones de <strong>{{ activeDriver?.fullName || 'chofer' }}</strong>.
            </p>
          </div>
        </div>

        <!-- Observaciones / Notas Adicionales (Opcional) -->
        <div class="form-group">
          <label class="form-label">Observaciones Adicionales (Opcional)</label>
          <input 
            v-model="formData.notes"
            type="text"
            class="form-control" 
            placeholder="Peajes, número de guía, condiciones especiales..."
          />
        </div>

        <!-- Botones de Acción -->
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="$emit('close')">
            Cancelar
          </button>
          <button type="submit" class="btn btn-primary" :disabled="loading">
            <span v-if="loading">Registrando...</span>
            <span v-else>Guardar Servicio</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.modal-header {
  padding: 20px 24px;
  border-bottom: 1px solid var(--brand-border);
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}

.badge-tag {
  font-size: 0.72rem;
  color: var(--brand-green-light);
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 4px;
}

.modal-title {
  font-size: 1.25rem;
  color: #ffffff;
  margin-bottom: 4px;
}

.modal-subtitle {
  font-size: 0.85rem;
  color: var(--text-muted);
}

.btn-close {
  background: transparent;
  border: none;
  font-size: 1.7rem;
  color: var(--text-muted);
  cursor: pointer;
  line-height: 1;
  padding: 4px;
}

.btn-close:hover {
  color: #ffffff;
}

.modal-body {
  padding: 20px 24px;
}

.alert-error {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid #ef4444;
  color: #fca5a5;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  margin-bottom: 16px;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-row {
  display: flex;
  gap: 14px;
}

.flex-1 { flex: 1; }
.flex-2 { flex: 2; }

.driver-fixed-pill {
  display: flex;
  align-items: center;
  gap: 10px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  padding: 10px 14px;
  margin-bottom: 16px;
}

.driver-fixed-pill .label {
  font-size: 0.8rem;
  color: var(--text-muted);
}

.driver-fixed-pill .name {
  font-weight: 700;
  color: #ffffff;
  font-size: 0.95rem;
}

.driver-fixed-pill .rate-badge {
  margin-left: auto;
  font-size: 0.75rem;
  background: rgba(34, 197, 94, 0.15);
  color: var(--brand-green-light);
  border: 1px solid rgba(34, 197, 94, 0.3);
  padding: 2px 8px;
  border-radius: var(--radius-full);
  font-weight: 700;
}

.price-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.currency-prefix {
  position: absolute;
  left: 14px;
  font-weight: 700;
  color: var(--brand-green-light);
  font-size: 0.95rem;
  pointer-events: none;
}

.price-input.with-prefix {
  padding-left: 54px;
}

.price-input {
  font-family: var(--font-heading);
  font-weight: 700;
  color: var(--brand-green-light);
  font-size: 1.1rem;
}

.field-hint {
  font-size: 0.75rem;
  color: #fbbf24;
  margin-top: 4px;
}

/* Caja de previsualización de comisión */
.earnings-preview-card {
  background: linear-gradient(135deg, rgba(21, 128, 61, 0.2), rgba(34, 197, 94, 0.05));
  border: 1px solid var(--brand-border-strong);
  border-radius: var(--radius-md);
  padding: 14px 18px;
  margin: 18px 0;
}

.preview-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.preview-badge {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--brand-green-light);
  text-transform: uppercase;
}

.rate-tag {
  background: var(--brand-green);
  color: #ffffff;
  padding: 2px 8px;
  border-radius: var(--radius-full);
  font-size: 0.75rem;
  font-weight: 700;
}

.preview-body {
  display: flex;
  align-items: center;
  gap: 16px;
}

.preview-amount {
  display: flex;
  align-items: baseline;
  font-family: var(--font-heading);
  font-weight: 800;
  color: #ffffff;
  text-shadow: 0 0 15px var(--brand-green-glow);
}

.preview-amount .currency {
  font-size: 1.1rem;
  color: var(--brand-green-light);
  margin-right: 2px;
}

.preview-amount .val {
  font-size: 1.7rem;
}

.preview-desc {
  font-size: 0.8rem;
  color: var(--text-muted);
  line-height: 1.3;
}

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid var(--brand-border);
}

@media (max-width: 600px) {
  .form-row {
    flex-direction: column;
    gap: 0;
  }
  .modal-body {
    padding: 16px;
  }
  .preview-body {
    flex-direction: column;
    align-items: flex-start;
    gap: 6px;
  }
}
</style>
