import { ref } from 'vue';

const STORAGE_KEY = 'tito_cash_reimbursed_services';

const loadOverrides = (): Record<string, boolean> => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
};

// Mapa reactivo compartido entre vistas para que cualquier cambio actualice KPIs y Planilla al instante
export const cashReimbursementOverrides = ref<Record<string, boolean>>(loadOverrides());

export interface ParsedServiceExpense {
  amount: number;
  detail: string;
  paidInCash: boolean;
}

export const parseServiceExpense = (notes?: string, serviceId?: number | string): ParsedServiceExpense => {
  if (!notes || !notes.trim()) {
    return { amount: 0, detail: '', paidInCash: false };
  }

  const raw = notes.trim();
  const expenseKeywords = /dieta|gasto|peaje|parqueo|desayuno|comida|cena|vi[aá]tico|combustible|utilic[eé]|pagu[eé]/i;
  let totalAmount = 0;

  const rdMatches = [...raw.matchAll(/RD\$\s*(\d+(?:,\d{3})*(?:\.\d+)?)/gi)];
  if (rdMatches.length > 0) {
    for (const m of rdMatches) {
      totalAmount += parseFloat(m[1].replace(/,/g, '')) || 0;
    }
  } else if (expenseKeywords.test(raw)) {
    const numMatches = [...raw.matchAll(/\b(\d{2,6}(?:\.\d{1,2})?)\b/g)];
    for (const m of numMatches) {
      totalAmount += parseFloat(m[1]) || 0;
    }
  }

  // Detectar si en la nota se marcó como pagado/entregado en efectivo
  const noteSaysCash = /\[PAGADO EN EFECTIVO\]|pagado en efectivo|entregado en efectivo|reembolsado en efectivo/i.test(raw);

  // Verificar si el administrador cambió el estado con 1 clic
  const key = serviceId !== undefined && serviceId !== null ? String(serviceId) : '';
  const hasOverride = key !== '' && key in cashReimbursementOverrides.value;
  const paidInCash = hasOverride ? cashReimbursementOverrides.value[key] : noteSaysCash;

  const cleanDetail = raw
    .replace(/\[PAGADO EN EFECTIVO\]\s*/gi, '')
    .replace(/\[PENDIENTE REEMBOLSO\]\s*/gi, '')
    .replace(/^Dietas\/Gastos:\s*/i, '')
    .trim();

  return {
    amount: totalAmount,
    detail: cleanDetail,
    paidInCash
  };
};

export const toggleServiceExpenseCashStatus = (service: { id?: number | string; notes?: string }): boolean => {
  if (service.id === undefined || service.id === null) return false;
  const current = parseServiceExpense(service.notes, service.id);
  const nextStatus = !current.paidInCash;
  const key = String(service.id);

  cashReimbursementOverrides.value = {
    ...cashReimbursementOverrides.value,
    [key]: nextStatus
  };

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(cashReimbursementOverrides.value));
  } catch {
    // ignore storage errors
  }

  return nextStatus;
};

export const clearServiceExpenseOverride = (serviceId?: number | string) => {
  if (serviceId === undefined || serviceId === null) return;
  const key = String(serviceId);
  if (key in cashReimbursementOverrides.value) {
    const next = { ...cashReimbursementOverrides.value };
    delete next[key];
    cashReimbursementOverrides.value = next;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(cashReimbursementOverrides.value));
    } catch {
      // ignore storage errors
    }
  }
};


