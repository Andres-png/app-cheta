import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { login, loginWithGoogle } from "../api"; // 👈 usamos el cliente api centralizado
import {
  Box,
  Paper,
  TextField,
  Button,
  Typography,
  Alert,
  Divider,
} from "@mui/material";
import googleOneTap from "google-one-tap";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  // Inicializar Google Identity Services
  useEffect(() => {
    let script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.defer = true;
    document.body.appendChild(script);

    script.onload = () => {
      window.google.accounts.id.initialize({  
        client_id:
          "171570749927-7rjns7284seuka5ab7pp2uss1map1ghl.apps.googleusercontent.com", // 👈 tu ClientId real
        callback: handleGoogleResponse,
      });

      window.google.accounts.id.renderButton(
        document.getElementById("googleBtn"),
        { theme: "outline", size: "large" }
      );
    };
  }, []);

  // Procesar la respuesta de Google
  const handleGoogleResponse = async (response) => {
    try {
      const data = await loginWithGoogle(response.credential);

      localStorage.setItem("token", data.token);
      if (data.username) {
        localStorage.setItem("username", data.username);
      }

      navigate("/form", { replace: true });
    } catch (err) {
      console.error("❌ Error en login con Google:", err);
      setError("Error al iniciar sesión con Google");
    }
  };

  // Login normal
  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const res = await login(username, password);

      localStorage.setItem("token", res.token);
      localStorage.setItem("username", res.username);

      navigate("/form", { replace: true });
    } catch (err) {
      console.error("❌ Error en login normal:", err);
      setError("Usuario o contraseña incorrectos");
    } finally {
      setLoading(false);
    }
  }

return (
  <Box
    sx={{
      position: "fixed",         // se pega a la pantalla
      top: 0,
      left: 0,
      width: "100%",
      height: "100%",
      display: "flex",
      justifyContent: "center",  // centra horizontal
      alignItems: "center",      // centra vertical
      backgroundColor: "background.default", // opcional, por si necesitas fondo
      p: 2,
    }}
  > 
    <Paper
      elevation={3}
      sx={{
        p: { xs: 2, sm: 4 },
        width: "100%",
        maxWidth: 400,
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
      <form onSubmit={handleSubmit}>
        <TextField
          label="Usuario"
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
          fullWidth
          sx={{ mt: 2 }}
          disabled={loading}
        >
          {loading ? "Cargando..." : "Entrar"}
        </Button>
      </form>

      <Divider sx={{ my: 3 }}>O</Divider>

      {/* LOGIN CON GOOGLE */}
      <div
        id="googleBtn"
        style={{ display: "flex", justifyContent: "center" }}
      ></div>
    </Paper>
  </Box>
);

}
