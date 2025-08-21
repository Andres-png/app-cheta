import React, { useEffect, useState } from "react";
import { listContactos, deleteContacto } from "../api";
import {
  Box, Typography, Alert, Paper, Accordion, AccordionSummary, AccordionDetails,
  Avatar, Divider, TextField, IconButton, Stack
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import PersonIcon from "@mui/icons-material/Person";
import { orange, red } from "@mui/material/colors";
import { Edit, Delete } from "@mui/icons-material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import { useNavigate } from "react-router-dom";

export default function DataPage() {
  const [contactos, setContactos] = useState([]);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const navigate = useNavigate();

  const openWhatsApp = (telefono) => {
    if (!telefono || telefono.trim() === "") return;
    let clean = telefono.replace(/\D/g, "");
    if (clean.length < 10) { alert("El número debe tener al menos 10 dígitos"); return; }
    if (!clean.startsWith("57")) clean = "57" + clean;
    const message = encodeURIComponent("hola como estas?");
    window.open(`https://wa.me/${clean}?text=${message}`, "_blank");
  };

  const cargarContactos = () => {
    listContactos()
      .then(setContactos)
      .catch((err) => setError(err.message || "Error al cargar contactos"));
  };

  useEffect(() => { cargarContactos(); }, []);

  const contactosFiltrados = contactos.filter((c) =>
    (c.nombre || "").toLowerCase().includes(search.toLowerCase()) ||
    (c.email || "").toLowerCase().includes(search.toLowerCase()) ||
    (c.telefono || "").toLowerCase().includes(search.toLowerCase())
  );

  const onEliminar = async (id) => {
    if (!window.confirm("¿Seguro que deseas eliminar este contacto?")) return;
    try {
      await deleteContacto(id);
      cargarContactos();
    } catch (err) {
      alert(err.message || "Error al eliminar contacto");
    }
  };

  return (
    <Box sx={{bgcolor: "#f5f6fa", p: 2, display: "flex", flexDirection: "column", alignItems: "center",       position: "fixed",         // se pega a la pantalla
      left: 0,
      width: "100%",
      height: "100%", }}>
      <Box sx={{ mb: 2 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: "bold", color: "#333", display: "flex", alignItems: "center", gap: 1 }}>
          📇 Mis Contactos
        </Typography>
        <TextField size="small" variant="outlined" placeholder="Buscar contacto..." value={search} onChange={(e) => setSearch(e.target.value)} />
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {contactosFiltrados.length === 0 ? (
        <Alert severity="info">No hay contactos que coincidan.</Alert>
      ) : (
        contactosFiltrados.map((c) => (
          <Paper key={c.id} elevation={3} sx={{ maxWidth: 500, width: "100%", mb: 1.5, borderRadius: 2, overflow: "hidden", transition: "transform 0.2s", "&:hover": { transform: "scale(1.02)" } }}>
            <Accordion sx={{ boxShadow: "none" }}>
              <AccordionSummary expandIcon={<ExpandMoreIcon />} sx={{ bgcolor: "#fff", display: "flex", alignItems: "center", px: 1.5, py: 0.5 }}>
                <Avatar sx={{ bgcolor: "#1976d2", width: 36, height: 36, mr: 1.5 }}><PersonIcon fontSize="small" /></Avatar>
                <Typography sx={{ fontWeight: "bold", fontSize: "1rem" }}>{c.nombre}</Typography>
              </AccordionSummary>
              <Divider />
              <AccordionDetails sx={{ bgcolor: "#fafafa", px: 3, py: 2 }}>
                <Typography variant="body2" sx={{ mb: 1 }}><strong>Email:</strong> {c.email}</Typography>
                <Typography variant="body2" sx={{ mb: 1 }}><strong>Teléfono:</strong> {c.telefono || "No registrado"}</Typography>
                <Typography variant="body2"><strong>Nota:</strong> {c.nota || "Sin nota"}</Typography>
                <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
                  <IconButton onClick={() => navigate(`/form/${c.id}`)} sx={{ backgroundColor: orange[500], color: "white", "&:hover": { backgroundColor: orange[700] } }}>
                    <Edit />
                  </IconButton>
                  <IconButton onClick={() => onEliminar(c.id)} sx={{ backgroundColor: red[500], color: "white", "&:hover": { backgroundColor: red[700] } }}>
                    <Delete />
                  </IconButton>
                  <IconButton
                    onClick={() => openWhatsApp(c.telefono)}
                    sx={{ backgroundColor: "#25D366", color: "white", borderRadius: "8px", p: "8px", "&:hover": { backgroundColor: "#128C7E" }, "&:disabled": { backgroundColor: "#ccc" } }}
                    disabled={!c.telefono}
                    title={c.telefono ? "Enviar WhatsApp" : "Sin teléfono registrado"}
                  >
                    <WhatsAppIcon />
                  </IconButton>
                </Stack>
              </AccordionDetails>
            </Accordion>
          </Paper>
        ))
      )}
    </Box>
  );
}
