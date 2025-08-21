// src/api.js
const API_URL = "http://localhost:5174"; // AJUSTA si tu backend usa otro puerto/protocolo

export async function api(path, options = {}) {
  const token = localStorage.getItem("token");
  const headers = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const res = await fetch(`${API_URL}${path}`, { ...options, headers });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || "Error en la petición");
  }
  return res.json();
}

export function getToken() {
  return localStorage.getItem("token");
}

export function clearAuth() {
  localStorage.removeItem("token");
  localStorage.removeItem("username");
}

// ---- Auth ----
export function login(username, password) {
  return api("/api/login", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });
}

export function register(username, password) {
  return api("/api/register", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });
}

// GIS: envía credential (ID token) al backend
export function loginWithGoogle(idToken) {
  return api("/api/auth/google", {
    method: "POST",
    body: JSON.stringify({ token: idToken }),
  });
}

// ---- Form CRUD ----
export function createContacto(body) {
  return api("/api/form", { method: "POST", body: JSON.stringify(body) });
}
export function listContactos() {
  return api("/api/form");
}
export function getContacto(id) {
  return api(`/api/form/${id}`);
}
export function updateContacto(id, body) {
  return api(`/api/form/${id}`, { method: "PUT", body: JSON.stringify(body) });
}
export function deleteContacto(id) {
  return api(`/api/form/${id}`, { method: "DELETE" });
}
