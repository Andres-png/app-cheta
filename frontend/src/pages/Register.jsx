import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api";
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
      await api("/api/register", {
        method: "POST",
        body: JSON.stringify({ username, password }),
      });

      // Login automático
      const res = await api("/api/login", {
        method: "POST",
        body: JSON.stringify({ username, password }),
      });

      localStorage.setItem("token", res.token);
      navigate("/form", { replace: true });
    } catch (err) {
      setError(err.message || "Error en el registro");
    }
  }

  return (
    <Box
                display="flex"
      justifyContent="center"
      alignItems="center"
      flexGrow={1}
      sx={{
        minHeight: "calc(100vh - 64px)",
        p: 2,
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

        <form onSubmit={handleSubmit} sx={{        }}>
          <TextField
            label="Usuario"
            variant="outlined"
            fullWidth
            margin="normal"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
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
          />
          <Button
            type="submit"
            variant="contained"
            color="primary"
            fullWidth
            sx={{ mt: 2 }}
          >
            Registrarse
          </Button>
        </form>
      </Paper>
    </Box>
  );
}
