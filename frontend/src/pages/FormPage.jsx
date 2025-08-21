import React, { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createContacto, getContacto, updateContacto } from "../api";
import { Box, Paper, Typography, TextField, Button, Alert } from "@mui/material";

export default function FormPage() {
  const { id } = useParams();
  const [nombre, setNombre] = useState("");
  const [email, setEmail] = useState("");
  const [telefono, setTelefono] = useState("");
  const [nota, setNota] = useState("");
  const [mensaje, setMensaje] = useState("");
  const [error, setError] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (!id) return;
    (async () => {
      try {
        const data = await getContacto(id);
        setNombre(data.nombre || "");
        setEmail(data.email || "");
        setTelefono(data.telefono || "");
        setNota(data.nota || "");
      } catch (e) {
        setError(true);
        setMensaje(e.message || "Error al cargar el contacto");
      }
    })();
  }, [id]);

  async function handleSubmit(e) {
    e.preventDefault();
    setMensaje("");
    setError(false);

    try {
      const body = { nombre, email, telefono, nota };
      if (id) await updateContacto(id, body);
      else await createContacto(body);

      setMensaje(id ? "Contacto actualizado ✅" : "Contacto guardado ✅");
      setTimeout(() => navigate("/data"), 600);
    } catch (e) {
      setError(true);
      setMensaje(e.message || "Error al guardar");
    }
  }

return (
  <Box
    sx={{
      position: "fixed",        // fuerza a ocupar toda la ventana
      top: 0,
      left: 0,
      width: "100%",
      height: "100%",
      display: "flex",
      alignItems: "center",     // centra vertical
      justifyContent: "center", // centra horizontal
      p: 2,
       backgroundColor: "background.default"
    }}
  >
    <Paper
      elevation={3}
      sx={{
        p: { xs: 2, sm: 4 },
        width: "100%",
        maxWidth: 500,
        borderRadius: 3,
      }}
    >
      <Typography variant="h5" gutterBottom>
        {id ? "Editar Contacto" : "Nuevo Contacto"}
      </Typography>

      {mensaje && (
        <Alert severity={error ? "error" : "success"} sx={{ mb: 2 }}>
          {mensaje}
        </Alert>
      )}

      <form onSubmit={handleSubmit}>
        <TextField
          label="Nombre"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          fullWidth
          sx={{ mb: 2 }}
        />
        <TextField
          label="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          fullWidth
          sx={{ mb: 2 }}
        />
        <TextField
          label="Teléfono"
          value={telefono}
          onChange={(e) => setTelefono(e.target.value)}
          fullWidth
          sx={{ mb: 2 }}
        />
        <TextField
          label="Nota"
          value={nota}
          onChange={(e) => setNota(e.target.value)}
          multiline
          rows={3}
          fullWidth
          sx={{ mb: 3 }}
        />
        <Button
          variant="contained"
          color="primary"
          type="submit"
          fullWidth
        >
          {id ? "Actualizar" : "Guardar"}
        </Button>
      </form>
    </Paper>
  </Box>
);


}
