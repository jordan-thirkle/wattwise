const API = 'http://localhost:45892';

async function safeFetch(url: string, options?: RequestInit) {
  const res = await fetch(url, options);
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export async function fetchCurrent() {
  return safeFetch(`${API}/api/current`);
}

export async function fetchSummary() {
  return safeFetch(`${API}/api/summary`);
}

export async function fetchHistory(days: number) {
  return safeFetch(`${API}/api/history?days=${days}`);
}

export async function fetchSuggestions() {
  return safeFetch(`${API}/api/suggestions`);
}

export async function fetchPermissions() {
  return safeFetch(`${API}/api/permissions`);
}

export async function fetchSettings() {
  return safeFetch(`${API}/api/settings`);
}

export async function updateSettings(settings: Record<string, unknown>) {
  return safeFetch(`${API}/api/settings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(settings),
  });
}

export async function fetchLicenseStatus() {
  return safeFetch(`${API}/api/license/status`);
}

export async function validateLicenseKey(key: string) {
  return safeFetch(`${API}/api/license/validate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key }),
  });
}

export async function startTrial() {
  return safeFetch(`${API}/api/license/trial`, { method: 'POST' });
}
