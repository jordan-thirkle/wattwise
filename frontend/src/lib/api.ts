const API = 'http://localhost:45892';

export async function fetchCurrent() {
  const res = await fetch(`${API}/api/current`);
  return res.json();
}

export async function fetchSummary() {
  const res = await fetch(`${API}/api/summary`);
  return res.json();
}

export async function fetchHistory(days: number) {
  const res = await fetch(`${API}/api/history?days=${days}`);
  return res.json();
}

export async function fetchSparkline(minutes: number) {
  const res = await fetch(`${API}/api/sparkline?minutes=${minutes}`);
  return res.json();
}

export async function fetchSuggestions() {
  const res = await fetch(`${API}/api/suggestions`);
  return res.json();
}

export async function fetchPermissions() {
  const res = await fetch(`${API}/api/permissions`);
  return res.json();
}

export async function fetchSettings() {
  const res = await fetch(`${API}/api/settings`);
  return res.json();
}

export async function updateSettings(settings: any) {
  const res = await fetch(`${API}/api/settings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(settings),
  });
  return res.json();
}

export async function fetchLicenseStatus() {
  const res = await fetch(`${API}/api/license/status`);
  return res.json();
}

export async function validateLicenseKey(key: string) {
  const res = await fetch(`${API}/api/license/validate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key }),
  });
  return res.json();
}

export async function startTrial() {
  const res = await fetch(`${API}/api/license/trial`, { method: 'POST' });
  return res.json();
}
