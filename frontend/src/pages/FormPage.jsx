import React, { useState, useEffect } from 'react';
import { api } from '../api';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Alert
} from '@mui/material';

export default function FormPage() {
  const { id } = useParams();
  const [nombre, setNombre] = useState('');
  const [email, setEmail] = useState('');
  const [telefono, setTelefono] = useState('');
  const [nota, setNota] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [error, setError] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (id) {
      api(`z/${id}`)
        .then((data) => {
          setNombre(data.nombre || '');
          setEmail(data.email || '');
          setTelefono(data.telefono || '');
          setNota(data.nota || '');
        })
        .catch((err) => {
          setError(true);
          setMensaje(err.message || 'Error al cargar el contacto');
        });
    }
  }, [id]);

  async function handleSubmit(e) {
    e.preventDefault();
    setMensaje('');
    setError(false);

    try {
      await api(id ? `z/${id}` : '/api/form', {
        method: id ? 'PUT' : 'POST',
        body: JSON.stringify({ nombre, email, telefono, nota })
      });

      setMensaje(id ? 'Contacto actualizado ✅' : 'Contacto guardado ✅');
      setTimeout(() => navigate('/data'), 1200);
    } catch (err) {
      setError(true);
      setMensaje(err.message || 'Error al guardar');
    }
  }

  return (
    <Box
      sx={{
        flexGrow: 1,

        height: 'calc(100vh - 64px)', // para compensar el Navbar
        p: 2,
      }}
    >
      <Paper
        elevation={3}
        sx={{
          p: { xs: 2, sm: 4 },
          width: '100%',
          maxWidth: '500px',
          borderRadius: 3,

        }}
      >
        <Typography variant="h5" gutterBottom>
          {id ? 'Editar Contacto' : 'Nuevo Contacto'}
        </Typography>

        {mensaje && (
          <Alert severity={error ? 'error' : 'success'} sx={{ mb: 2 }}>
            {mensaje}
          </Alert>
        )}

        <form onSubmit={handleSubmit} style={{ width: '100%' }}>
          <TextField label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} required fullWidth sx={{ mb: 2 }} />
          <TextField label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required fullWidth sx={{ mb: 2 }} />
          <TextField label="Teléfono" value={telefono} onChange={(e) => setTelefono(e.target.value)} fullWidth sx={{ mb: 2 }} />
          <TextField label="Nota" value={nota} onChange={(e) => setNota(e.target.value)} multiline rows={3} fullWidth sx={{ mb: 3 }} />
          <Button variant="contained" color="primary" type="submit" fullWidth>
            {id ? 'Actualizar' : 'Guardar'}
          </Button>
        </form>
      </Paper>
    </Box>
  );
}
