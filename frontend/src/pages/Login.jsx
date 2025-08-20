import React, { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { api } from "../api";
import {
  Box,
  Paper,
  TextField,
  Button,
  Typography,
  Alert,
  Divider,
} from "@mui/material";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  // Procesar parámetros de Google OAuth
  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const token = params.get("token");
    const googleUser = params.get("username");
    const errorParam = params.get("error");

    console.log("Parámetros recibidos:", { token, googleUser, error: errorParam });

    // Si hay un error de Google
    if (errorParam) {
      let errorMessage = "Error en autenticación con Google";
      switch (errorParam) {
        case "google_auth_failed":
          errorMessage = "Error en la autenticación con Google";
          break;
        case "no_email":
          errorMessage = "No se pudo obtener el email de Google";
          break;
        case "callback_exception":
          errorMessage = "Error interno en el callback de Google";
          break;
        default:
          errorMessage = `Error: ${errorParam}`;
      }
      setError(errorMessage);
      
      // Limpiar la URL
      window.history.replaceState({}, '', location.pathname);
      return;
    }

    // Si viene un token de Google exitoso
    if (token && token.trim() !== "") {
      console.log("✅ Token recibido de Google:", token.substring(0, 20) + "...");
      
      localStorage.setItem("token", token);
      if (googleUser) {
        localStorage.setItem("username", decodeURIComponent(googleUser));
      }
      
      console.log("✅ Login con Google exitoso");
      navigate("/form", { replace: true });
    }
  }, [location, navigate]);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      console.log("Enviando login normal:", { username, password: "***" });

      const res = await api("/api/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      console.log("Respuesta del backend login normal:", res);

      if (!res || !res.token) {
        throw new Error("No se recibió token en la respuesta");
      }

      localStorage.setItem("token", res.token);
      localStorage.setItem("username", res.username);

      console.log("✅ Login normal exitoso");
      navigate("/form", { replace: true });
    } catch (err) {
      console.error("❌ Error en login normal:", err);
      setError("Usuario o contraseña incorrectos");
    } finally {
      setLoading(false);
    }
  }

  // Redirigir a login de Google usando el endpoint del backend
  const handleGoogleLogin = () => {
    console.log("🔄 Redirigiendo a Google login...");
    setError(""); // Limpiar errores previos
    
    // Cambiar el puerto según donde esté corriendo tu backend
    // Puede ser 5000, 7000, 7001, etc. - verifica en tu terminal
    window.location.href = "/api/auth/google-login"; // Usar URL relativa
  };

  return (
    <Box
          display="flex"
      justifyContent="center"
      alignItems="center"
      sx={{
        p: 2,
      }}
    >
      <Paper
        elevation={6}
        sx={{
          p: { xs: 2, sm: 4 },
          width: "100%",
          borderRadius: 3,
       }}
      >
        <Typography variant="h5" gutterBottom textAlign="center">
          Iniciar Sesión
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {/* FORMULARIO LOGIN NORMAL */}
        <form onSubmit={handleSubmit} sx={{display:'flex'}} >
          <TextField
            label="Usuario"
            variant="outlined"
            fullWidth
            margin="normal"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
            disabled={loading}
          />
          <TextField
            label="Contraseña"
            type="password"
            variant="outlined"
            fullWidth
            margin="normal"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            disabled={loading}
          />
          <Button
            type="submit"
            variant="contained"
            color="primary"
            fullWidth
            sx={{ mt: 2 }}
            disabled={loading}
          >
            {loading ? "Cargando..." : "Entrar"}
          </Button>
        </form>

        <Divider sx={{ my: 3 }}>O</Divider>

        {/* LOGIN CON GOOGLE */}
        <Button
          variant="outlined"
          color="secondary"
          fullWidth
          onClick={handleGoogleLogin}
          disabled={loading}
          sx={{
            textTransform: "none",
            fontSize: "1rem",
          }}
        >
          🔍 Iniciar sesión con Google
        </Button>

        <Typography 
          variant="body2" 
          color="text.secondary" 
          textAlign="center" 
          sx={{ mt: 2 }}
        >
          ¿No tienes cuenta? Regístrate primero
        </Typography>
      </Paper>
    </Box>
  );
}