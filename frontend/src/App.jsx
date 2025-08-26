// App.jsx
import React from "react";
import {
  Routes,
  Route,
  Navigate,
  Link as RouterLink,
  useNavigate,
} from "react-router-dom";
import Login from "./pages/Login";
import Register from "./pages/Register";
import FormPage from "./pages/FormPage";
import DataPage from "./pages/DataPage";

// Material UI
import AppBar from "@mui/material/AppBar";
import Toolbar from "@mui/material/Toolbar";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import { red } from '@mui/material/colors';

// 🔹 Función para obtener el token
function getToken() {
  return localStorage.getItem("token");
}

import PropTypes from "prop-types";

function PrivateRoute({ children }) {
  const token = getToken();
  return token ? children : <Navigate to="/login" />;
}

PrivateRoute.propTypes = {
  children: PropTypes.node.isRequired,
};

function Navbar() {
  const navigate = useNavigate();
  const token = getToken();

  function handleLogout() {
    localStorage.removeItem("token");
    navigate("/login");
  }

  return (
    <AppBar position="fixed" color="primary" sx={{ width: "100%"}}>
      <Toolbar>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Mi Aplicación
        </Typography>
        {token ? (
          <>
            <Button color="inherit" component={RouterLink} to="/form">
              Nuevo Contacto
            </Button>
            <Button color="inherit" component={RouterLink} to="/data">
              Contactos
            </Button>
            <Button
              variant="contained"
              onClick={handleLogout}
              sx={{ bgcolor: red[800], "&:hover": { bgcolor: red[500] } }}
            >
              Cerrar sesión
            </Button>
          </>
        ) : (
          <>
            <Button color="inherit" component={RouterLink} to="/login">
              Login
            </Button>
            <Button color="inherit" component={RouterLink} to="/register">
              Registro
            </Button>
          </>
        )}
      </Toolbar>
    </AppBar>
  );
}

export default function App() {
return (
  <Box
    sx={{
      display: "flex",
      flexDirection: "column",
      minHeight: "100vh",
    }}
  >
    <Navbar />
    {/* Espaciador para compensar AppBar fijo */}
    <Box sx={{ height: 64 }} />
    
    {/* Contenedor central */}
    <Box
      sx={{
        flexGrow: 1,
        display: "flex",
        justifyContent: "center", // centra horizontal
        alignItems: "center",     // centra vertical
      }}
    >
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />

        {/* Crear contacto */}
        <Route
          path="/form"
          element={
            <PrivateRoute>
              <FormPage />
            </PrivateRoute>
          }
        />

        {/* Editar contacto */}
        <Route
          path="/form/:id"
          element={
            <PrivateRoute>
              <FormPage />
            </PrivateRoute>
          }
        />

        {/* Listado */}
        <Route
          path="/data"
          element={
            <PrivateRoute>
              <DataPage />
            </PrivateRoute>
          }
        />

        <Route path="/" element={<Navigate to="/login" />} />
      </Routes>
    </Box>
  </Box>
);

}
