# Jot

<div align="center">
  <img src="Assets/Square150x150Logo.scale-200.png" alt="Logo de Jot" width="100"/>
  
  **Jot es una aplicación para crear apuntes interactivos, desarrollada con WinUI 3 y .NET 8**
  
  [![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
  [![WinUI 3](https://img.shields.io/badge/WinUI-3.0-purple.svg)](https://docs.microsoft.com/en-us/windows/apps/winui/)
  [![Windows](https://img.shields.io/badge/Plataforma-Windows%2010/11-green.svg)](https://www.microsoft.com/windows/)
</div>

## Características

### **Funciones de Edición**
- **Editor de Markdown en tiempo real** con resaltado de sintaxis
- **Vista previa en vivo** del contenido formateado
- **Vista dividida** para edición y vista previa simultáneas
- **Barra de herramientas con atajos de formato** (Negrita, Cursiva, Código, Encabezados, Listas, etc.)
- **Soporte avanzado de Markdown** incluyendo:
  - Encabezados (H1-H6)
  - Listas (viñetas, numeradas, tareas)
  - Bloques de código con resaltado de sintaxis
  - Citas y comillas
  - Tablas y líneas horizontales
  - Formato en línea (negrita, cursiva, tachado, resaltado)

### **Tipos de Contenido Avanzados**
- **Imágenes**: Soporte para sintaxis `![texto alt](url-imagen)` con vista previa visual
- **Preguntas de Quiz**: Bloques de quiz interactivos con:
  - Preguntas de opción múltiple
  - Respuestas plegables con explicaciones
  - Resaltado visual e iconos
- **Secciones Plegables**: Contenido expandible/plegable usando etiquetas `<details>`
- **Hipervínculos**: Enlaces clicables con indicadores visuales

### **Gestión de Documentos**
- **Crear, editar, guardar y eliminar documentos**
- **Funcionalidad de guardado automático** con marcas de tiempo de modificación
- **Búsqueda de documentos** por título y contenido
- **Almacenamiento persistente** usando archivos JSON locales
- **Biblioteca de documentos** con lista de documentos recientes

### **Navegación y Organización**
- **Índice de Documento**: Tabla de contenidos auto-generada desde encabezados
- **Navegación jerárquica** con niveles de encabezados indentados
- **Funcionalidad de clic para navegar** para navegación rápida del documento
- **Alternar barra lateral** para escritura enfocada

### **Interfaz de Usuario**
- **Diseño Fluent moderno** con WinUI 3
- **Fondo Mica** para integración con Windows 11
- **Tres modos de vista**:
  - **Modo Edición**: Experiencia de edición de texto pura
  - **Modo Vista Previa**: Vista de Markdown renderizado
  - **Modo Dividido**: Edición y vista previa lado a lado
- **Diseño responsivo** con paneles plegables
- **Soporte de tema oscuro/claro** (consciente del sistema)

### **Rendimiento y Confiabilidad**
- **Renderizado en tiempo real** del contenido Markdown
- **Análisis e indexación eficiente** de documentos
- **Manejo de errores** con respaldos elegantes
- **Almacenamiento de documentos eficiente en memoria**

## Stack Tecnológico

- **Framework**: .NET 8.0
- **Framework de UI**: WinUI 3 (Windows App SDK 1.8)
- **Arquitectura**: MVVM (Model-View-ViewModel)
- **Lenguaje**: C# 12
- **Almacenamiento de Datos**: Archivos JSON (Newtonsoft.Json)
- **Toolkit MVVM**: CommunityToolkit.Mvvm 8.4.0
- **Plataformas de Destino**: Windows 10 (19041+), Windows 11

### Dependencias
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250916003" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

## Requisitos del Sistema

### Requisitos Mínimos
- **SO**: Windows 10 versión 2004 (19041) o posterior
- **Runtime**: .NET 8.0 Runtime
- **Arquitectura**: x86, x64, o ARM64

### Recomendado
- **SO**: Windows 11 22H2 o posterior
- **RAM**: 4GB o más
- **Almacenamiento**: 100MB de espacio libre

## Instalación y Configuración

### Prerequisitos
1. **Instalar .NET 8.0 SDK**
   ```bash
   # Descargar desde: https://dotnet.microsoft.com/download/dotnet/8.0
   # O usando winget:
   winget install Microsoft.DotNet.SDK.8
   ```

2. **Instalar Windows App SDK** (usualmente incluido con .NET 8 SDK)

### Compilar desde el Código Fuente

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/davidcabrero/Jot.git
   cd Jot
   ```

2. **Restaurar dependencias**
   ```bash
   dotnet restore
   ```

3. **Compilar la aplicación**
   ```bash
   # Para desarrollo
   dotnet build
   
   # Para release
   dotnet build -c Release
   ```

4. **Ejecutar la aplicación**
   ```bash
   # Ejecutar con runtime específico
   dotnet run --runtime win-x64
   
   # O ejecutar el ejecutable directamente
   .\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Jot.exe
   ```

## Guía de Uso

### Creando Tu Primer Documento

1. **Lanzar Jot**
2. **Hacer clic en "Crear Nuevo Documento"** o usar el botón + en la barra de herramientas
3. **Comenzar a escribir** tu contenido usando sintaxis Markdown
4. **Usar la barra de herramientas** para opciones de formato rápido
5. **Cambiar entre modos de vista** usando los botones Editar/Vista Previa/Dividir

## Estructura del Proyecto

```
Jot/
├── Assets/                 # Iconos y recursos de aplicación
├── Controls/              # Controles personalizados de WinUI
│   ├── RichTextEditor.xaml    # Editor de Markdown con barra de herramientas
│   ├── MarkdownPreview.xaml   # Visor de Markdown renderizado
│   └── QuizControl.xaml       # Componente de quiz
├── Converters/            # Convertidores de valor XAML
├── Models/                # Modelos de datos
│   └── Document.cs           # Entidad documento
├── Services/              # Servicios de lógica de negocio
│   ├── DocumentService.cs    # Operaciones CRUD de documento
│   └── ThemeService.cs       # Gestión de temas
├── ViewModels/            # View models MVVM
│   ├── MainViewModel.cs      # Lógica de ventana principal
│   └── SettingsViewModel.cs  # Gestión de configuraciones
├── MainWindow.xaml        # Ventana principal de aplicación
├── App.xaml              # Configuración de aplicación
└── Jot.csproj           # Archivo de proyecto
```

## Configuración

### Almacenamiento de Documentos
Los documentos se almacenan localmente en:
```
%LOCALAPPDATA%/Packages/[AppPackageId]/LocalState/Documents/
```

Cada documento se guarda como un archivo JSON con el formato:
```json
{
  "Id": "guid-aquí",
  "Title": "Título del Documento",
  "Content": "Contenido markdown...",
  "CreatedAt": "2024-01-01T00:00:00Z",
  "ModifiedAt": "2024-01-01T00:00:00Z",
  "Tags": [],
  "FolderPath": "",
  "Type": "Markdown"
}
```