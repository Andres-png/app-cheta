import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { register, login } from "../api"; // 👈 usamos api centralizada
import {
  Box,
  Paper,
  TextField,
  Button,
  Typography,
  Alert,
} from "@mui/material";

export default function Register() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const navigate = useNavigate();

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");

    try {
      // Registro
      await register(username, password);

      // Login automático
      const res = await login(username, password);

      localStorage.setItem("token", res.token);
      localStorage.setItem("username", res.username);

      navigate("/form", { replace: true });
    } catch (err) {
      setError(err.message || "Error en el registro");
    }
  }

return (
  <Box
    sx={{
      position: "fixed",        // fuerza a ocupar toda la pantalla
      top: 0,
      left: 0,
      width: "100%",
      height: "100%",
      display: "flex",
      justifyContent: "center", // centra horizontal
      alignItems: "center",     // centra vertical
      p: 2,
       backgroundColor: "background.default"
    }}
  >
    <Paper
      elevation={6}
      sx={{
        p: { xs: 2, sm: 4 },
        width: "100%",
        maxWidth: 500,
        borderRadius: 3,
      }}
    >
      <Typography variant="h5" gutterBottom textAlign="center">
        Registro
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit}>
        <TextField
          label="Usuario"
          fullWidth
          margin="normal"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
        />
        <TextField
          label="Contraseña"
          type="password"
          fullWidth
          margin="normal"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }}>
          Registrarse
        </Button>
      </form>
    </Paper>
  </Box>
);

}
