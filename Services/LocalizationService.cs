using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Jot.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService _instance;
        private Dictionary<string, Dictionary<string, string>> _localizedStrings;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Dictionary<string, string> _supportedLanguages = new()
        {
            { "en", "English" },
            { "es", "Español" },
            { "ca", "Català" },
            { "ast", "Asturianu" }
        };

        public Dictionary<string, string> SupportedLanguages => _supportedLanguages;

        private string _currentLanguage = "en";
        public string CurrentLanguage 
        { 
            get => _currentLanguage;
            private set
            {
                _currentLanguage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguageName)));
            }
        }

        public string CurrentLanguageName => _supportedLanguages.TryGetValue(CurrentLanguage, out var name) ? name : "English";

        private LocalizationService()
        {
            InitializeLocalizedStrings();

            // Establecer idioma inicial (inglés por defecto)
            CurrentLanguage = "en";
        }

        private void InitializeLocalizedStrings()
        {
            _localizedStrings = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["AppTitle"] = "Jot - Modern Note Taking",
                    ["Documents"] = "Documents",
                    ["NewDocument"] = "New Document",
                    ["SaveDocument"] = "Save Document",
                    ["DeleteDocument"] = "Delete Document",
                    ["ExportToHtml"] = "Export to HTML",
                    ["UploadToGitHub"] = "Upload to GitHub",
                    ["Search"] = "Search",
                    ["Language"] = "Language",
                    ["EditMode"] = "Edit",
                    ["PreviewMode"] = "Preview",
                    ["SplitMode"] = "Split",
                    ["VoiceRecognition"] = "Voice Recognition",
                    ["Listening"] = "🎤 Listening... Speak now",
                    ["VoiceRecognitionStopped"] = "🔇 Voice recognition stopped",
                    ["ReadingText"] = "🔊 Reading Text",
                    ["PlayingTextAloud"] = "Playing text aloud...",
                    ["Cancel"] = "Cancel",
                    ["Stop"] = "Stop",
                    ["OK"] = "OK",
                    ["FocusModeActivated"] = "🎯 Focus Mode Activated",
                    ["FocusModeDeactivated"] = "🎯 Focus Mode Deactivated",
                    ["FocusModeDescription"] = "Simplified interface for concentration.\nPress F11 to exit.",
                    ["FocusModeRestored"] = "Complete interface restored.",
                    ["FontSize"] = "📏 Font Size",
                    ["CurrentSize"] = "Current size: {0}pt",
                    ["PomodoroCompleted"] = "🍅 Pomodoro Completed!",
                    ["PomodoroCompletedDescription"] = "Great job! You've completed a 25-minute focus session.\n\nTime for a 5-minute break?",
                    ["StartBreak"] = "Start Break",
                    ["ContinueWriting"] = "Continue Writing",
                    ["Close"] = "Close",
                    ["AdvancedSearch"] = "Advanced Search",
                    ["SearchFor"] = "Search for:",
                    ["SearchIn"] = "Search in:",
                    ["CurrentDocument"] = "Current Document",
                    ["AllDocuments"] = "All Documents",
                    ["SelectedDocuments"] = "Selected Documents",
                    ["UseRegularExpressions"] = "Use regular expressions",
                    ["WholeWordsOnly"] = "Whole words only",
                    ["FindAndReplace"] = "Find and Replace",
                    ["Find"] = "Find:",
                    ["ReplaceWith"] = "Replace with:",
                    ["CaseSensitive"] = "Case sensitive",
                    ["ReplaceAll"] = "Replace All",
                    ["FindNext"] = "Find Next",
                    ["SearchResult"] = "🔍 Search Result",
                    ["FoundAtPosition"] = "Found at position {0}",
                    ["NotFound"] = "'{0}' not found",
                    ["ReplacementsCompleted"] = "{0} replacements completed",
                    ["DocumentStatistics"] = "📊 Document Statistics",
                    ["CompleteStatistics"] = "📊 Complete Statistics",
                    ["Content"] = "📝 Content:",
                    ["Words"] = "• Words: {0:N0}",
                    ["Characters"] = "• Characters: {0:N0}",
                    ["CharactersNoSpaces"] = "• Characters (no spaces): {0:N0}",
                    ["Lines"] = "• Lines: {0:N0}",
                    ["Paragraphs"] = "• Paragraphs: {0:N0}",
                    ["Sentences"] = "• Sentences: {0:N0}",
                    ["AvgWordsPerSentence"] = "• Average words/sentence: {0}",
                    ["EstimatedTime"] = "⏱️ Estimated Time:",
                    ["ReadingTime"] = "• Reading time: ~{0} min",
                    ["SpeakingTime"] = "• Speaking time: ~{0} min",
                    ["SessionStats"] = "📈 Session Stats:",
                    ["WordsWrittenToday"] = "• Words written today: {0:N0}",
                    ["WordsInSession"] = "• Words in session: {0:N0}",
                    ["NoDocumentSelected"] = "No Document Selected",
                    ["NoDocumentSelectedDescription"] = "Please select or create a document first before uploading to GitHub.",
                    ["Connected"] = "Connected",
                    ["NotConnected"] = "Not Connected",
                    ["LanguageChanged"] = "Language changed to {0}",
                    ["LastModified"] = "Last modified",
                    ["DocumentIndex"] = "Document Index",
                    ["WelcomeToJot"] = "Welcome to Jot",
                    ["CreateFirstDocument"] = "Create your first document to get started",
                    ["CreateNewDocument"] = "Create New Document",
                    ["GitHub"] = "GitHub",
                    ["ConnectGitHub"] = "Connect GitHub",
                    ["UploadCurrent"] = "Upload Current",
                    ["Repositories"] = "Repositories",
                    ["Disconnect"] = "Disconnect",
                    ["ExpandGitHubOptions"] = "Expand GitHub Options",
                    ["DocumentTitle"] = "Document title...",
                    ["SearchDocuments"] = "Search documents...",
                    ["WordsCount"] = "Words: {0}",
                    ["CharactersCount"] = "Characters: {0}",
                    ["ReadingTimeCount"] = "Reading time: {0} min",
                    ["LineColumn"] = "Line {0}, Column {1}",
                    ["ExportingToHtml"] = "Exporting to HTML...",
                    ["ToggleSidebar"] = "Toggle Sidebar",
                    ["QuickUploadToGitHub"] = "Quick Upload to GitHub",
                    ["GitHubSettings"] = "GitHub Settings",
                    ["AIAssistant"] = "AI Assistant",
                    ["PythonCodeExecution"] = "Python Code Execution",
                    ["Close"] = "Close",
                    ["Send"] = "Send",
                    ["EnterYourQuestion"] = "Enter your question...",
                    ["SuggestedQuestions"] = "Suggested questions:",
                    ["ExportDocument"] = "Export to HTML",
                    ["DeleteDocument"] = "Delete",
                    ["UploadDocument"] = "Upload to GitHub",
                    ["ChatbotWelcome"] = "Hi! I'm your document assistant. You can ask me about the content of your documents."
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["AppTitle"] = "Jot - Toma de Notas Moderna",
                    ["Documents"] = "Documentos",
                    ["NewDocument"] = "Nuevo Documento",
                    ["SaveDocument"] = "Guardar Documento",
                    ["DeleteDocument"] = "Eliminar Documento",
                    ["ExportToHtml"] = "Exportar a HTML",
                    ["UploadToGitHub"] = "Subir a GitHub",
                    ["Search"] = "Buscar",
                    ["Language"] = "Idioma",
                    ["EditMode"] = "Editar",
                    ["PreviewMode"] = "Vista Previa",
                    ["SplitMode"] = "Dividir",
                    ["VoiceRecognition"] = "Reconocimiento de Voz",
                    ["Listening"] = "🎤 Escuchando... Habla ahora",
                    ["VoiceRecognitionStopped"] = "🔇 Reconocimiento de voz detenido",
                    ["ReadingText"] = "🔊 Leyendo Texto",
                    ["PlayingTextAloud"] = "Reproduciendo texto en voz alta...",
                    ["Cancel"] = "Cancelar",
                    ["Stop"] = "Detener",
                    ["OK"] = "Aceptar",
                    ["FocusModeActivated"] = "🎯 Modo Enfoque Activado",
                    ["FocusModeDeactivated"] = "🎯 Modo Enfoque Desactivado",
                    ["FocusModeDescription"] = "Interfaz simplificada para concentración.\nPresiona F11 para salir.",
                    ["FocusModeRestored"] = "Interfaz completa restaurada.",
                    ["FontSize"] = "📏 Tamaño de Fuente",
                    ["CurrentSize"] = "Tamaño actual: {0}pt",
                    ["PomodoroCompleted"] = "🍅 ¡Pomodoro Completado!",
                    ["PomodoroCompletedDescription"] = "¡Buen trabajo! Has completado una sesión de concentración de 25 minutos.\n\n¿Es hora de un descanso de 5 minutos?",
                    ["StartBreak"] = "Iniciar Descanso",
                    ["ContinueWriting"] = "Continuar Escribiendo",
                    ["Close"] = "Cerrar",
                    ["AdvancedSearch"] = "Búsqueda Avanzada",
                    ["SearchFor"] = "Buscar:",
                    ["SearchIn"] = "Buscar en:",
                    ["CurrentDocument"] = "Documento Actual",
                    ["AllDocuments"] = "Todos los Documentos",
                    ["SelectedDocuments"] = "Documentos Seleccionados",
                    ["UseRegularExpressions"] = "Usar expresiones regulares",
                    ["WholeWordsOnly"] = "Solo palabras completas",
                    ["FindAndReplace"] = "Buscar y Reemplazar",
                    ["Find"] = "Buscar:",
                    ["ReplaceWith"] = "Reemplazar con:",
                    ["CaseSensitive"] = "Distinguir mayúsculas y minúsculas",
                    ["ReplaceAll"] = "Reemplazar Todo",
                    ["FindNext"] = "Buscar Siguiente",
                    ["SearchResult"] = "🔍 Resultado de Búsqueda",
                    ["FoundAtPosition"] = "Encontrado en posición {0}",
                    ["NotFound"] = "'{0}' no encontrado",
                    ["ReplacementsCompleted"] = "Se realizaron {0} reemplazos",
                    ["DocumentStatistics"] = "📊 Estadísticas del Documento",
                    ["CompleteStatistics"] = "📊 Estadísticas Completas",
                    ["Content"] = "📝 Contenido:",
                    ["Words"] = "• Palabras: {0:N0}",
                    ["Characters"] = "• Caracteres: {0:N0}",
                    ["CharactersNoSpaces"] = "• Caracteres (sin espacios): {0:N0}",
                    ["Lines"] = "• Líneas: {0:N0}",
                    ["Paragraphs"] = "• Párrafos: {0:N0}",
                    ["Sentences"] = "• Oraciones: {0:N0}",
                    ["AvgWordsPerSentence"] = "• Promedio palabras/oración: {0}",
                    ["EstimatedTime"] = "⏱️ Tiempo Estimado:",
                    ["ReadingTime"] = "• Tiempo de lectura: ~{0} min",
                    ["SpeakingTime"] = "• Tiempo de oratoria: ~{0} min",
                    ["SessionStats"] = "📈 Estadísticas de Sesión:",
                    ["WordsWrittenToday"] = "• Palabras escritas hoy: {0:N0}",
                    ["WordsInSession"] = "• Palabras en esta sesión: {0:N0}",
                    ["NoDocumentSelected"] = "Ningún Documento Seleccionado",
                    ["NoDocumentSelectedDescription"] = "Por favor selecciona o crea un documento antes de subirlo a GitHub.",
                    ["Connected"] = "Conectado",
                    ["NotConnected"] = "No Conectado",
                    ["LanguageChanged"] = "Idioma cambiado a {0}",
                    ["LastModified"] = "Última modificación",
                    ["DocumentIndex"] = "Índice del Documento",
                    ["WelcomeToJot"] = "Bienvenido a Jot",
                    ["CreateFirstDocument"] = "Crea tu primer documento para comenzar",
                    ["CreateNewDocument"] = "Crear Nuevo Documento",
                    ["GitHub"] = "GitHub",
                    ["ConnectGitHub"] = "Conectar GitHub",
                    ["UploadCurrent"] = "Subir Actual",
                    ["Repositories"] = "Repositorios",
                    ["Disconnect"] = "Desconectar",
                    ["ExpandGitHubOptions"] = "Expandir Opciones de GitHub",
                    ["DocumentTitle"] = "Título del documento...",
                    ["SearchDocuments"] = "Buscar documentos...",
                    ["WordsCount"] = "Palabras: {0}",
                    ["CharactersCount"] = "Caracteres: {0}",
                    ["ReadingTimeCount"] = "Tiempo de lectura: {0} min",
                    ["LineColumn"] = "Línea {0}, Columna {1}",
                    ["ExportingToHtml"] = "Exportando a HTML...",
                    ["ToggleSidebar"] = "Alternar Barra Lateral",
                    ["QuickUploadToGitHub"] = "Subida Rápida a GitHub",
                    ["GitHubSettings"] = "Configuración de GitHub",
                    ["AIAssistant"] = "Asistente IA",
                    ["PythonCodeExecution"] = "Ejecución de Código Python",
                    ["Close"] = "Cerrar",
                    ["Send"] = "Enviar",
                    ["EnterYourQuestion"] = "Escribe tu pregunta...",
                    ["SuggestedQuestions"] = "Preguntas sugeridas:",
                    ["ExportDocument"] = "Exportar a HTML",
                    ["DeleteDocument"] = "Eliminar",
                    ["UploadDocument"] = "Subir a GitHub",
                    ["ChatbotWelcome"] = "¡Hola! Soy tu asistente de documentos. Puedes preguntarme sobre el contenido de tus documentos."
                },
                ["ca"] = new Dictionary<string, string>
                {
                    ["AppTitle"] = "Jot - Presa de Notes Moderna",
                    ["Documents"] = "Documents",
                    ["NewDocument"] = "Nou Document",
                    ["SaveDocument"] = "Desar Document",
                    ["DeleteDocument"] = "Eliminar Document",
                    ["ExportToHtml"] = "Exportar a HTML",
                    ["UploadToGitHub"] = "Pujar a GitHub",
                    ["Search"] = "Cercar",
                    ["Language"] = "Idioma",
                    ["EditMode"] = "Editar",
                    ["PreviewMode"] = "Vista Prèvia",
                    ["SplitMode"] = "Dividir",
                    ["VoiceRecognition"] = "Reconeixement de Veu",
                    ["Listening"] = "🎤 Escoltant... Parla ara",
                    ["VoiceRecognitionStopped"] = "🔇 Reconeixement de veu aturat",
                    ["ReadingText"] = "🔊 Llegint Text",
                    ["PlayingTextAloud"] = "Reproduint text en veu alta...",
                    ["Cancel"] = "Cancel·lar",
                    ["Stop"] = "Aturar",
                    ["OK"] = "D'acord",
                    ["FocusModeActivated"] = "🎯 Mode Concentració Activat",
                    ["FocusModeDeactivated"] = "🎯 Mode Concentració Desactivat",
                    ["FocusModeDescription"] = "Interfície simplificada per a la concentració.\nPrem F11 per sortir.",
                    ["FocusModeRestored"] = "Interfície completa restaurada.",
                    ["FontSize"] = "📏 Mida de Lletra",
                    ["CurrentSize"] = "Mida actual: {0}pt",
                    ["PomodoroCompleted"] = "🍅 Pomodoro Completat!",
                    ["PomodoroCompletedDescription"] = "Bona feina! Has completat una sessió de concentració de 25 minuts.\n\nÉs hora d'un descans de 5 minuts?",
                    ["StartBreak"] = "Iniciar Descans",
                    ["ContinueWriting"] = "Continuar Escrivint",
                    ["Close"] = "Tancar",
                    ["AdvancedSearch"] = "Cerca Avançada",
                    ["SearchFor"] = "Cercar:",
                    ["SearchIn"] = "Cercar a:",
                    ["CurrentDocument"] = "Document Actual",
                    ["AllDocuments"] = "Tots els Documents",
                    ["SelectedDocuments"] = "Documents Seleccionats",
                    ["UseRegularExpressions"] = "Usar expressions regulars",
                    ["WholeWordsOnly"] = "Només paraules completes",
                    ["FindAndReplace"] = "Cercar i Reemplaçar",
                    ["Find"] = "Cercar:",
                    ["ReplaceWith"] = "Reemplaçar amb:",
                    ["CaseSensitive"] = "Distingir majúscules i minúscules",
                    ["ReplaceAll"] = "Reemplaçar Tot",
                    ["FindNext"] = "Cercar Següent",
                    ["SearchResult"] = "🔍 Resultat de Cerca",
                    ["FoundAtPosition"] = "Trobat a la posició {0}",
                    ["NotFound"] = "'{0}' no trobat",
                    ["ReplacementsCompleted"] = "Es van realitzar {0} reemplaçaments",
                    ["DocumentStatistics"] = "📊 Estadístiques del Document",
                    ["CompleteStatistics"] = "📊 Estadístiques Completes",
                    ["Content"] = "📝 Contingut:",
                    ["Words"] = "• Paraules: {0:N0}",
                    ["Characters"] = "• Caràcters: {0:N0}",
                    ["CharactersNoSpaces"] = "• Caràcters (sense espais): {0:N0}",
                    ["Lines"] = "• Línies: {0:N0}",
                    ["Paragraphs"] = "• Paràgrafs: {0:N0}",
                    ["Sentences"] = "• Frases: {0:N0}",
                    ["AvgWordsPerSentence"] = "• Mitjana paraules/frase: {0}",
                    ["EstimatedTime"] = "⏱️ Temps Estimat:",
                    ["ReadingTime"] = "• Temps de lectura: ~{0} min",
                    ["SpeakingTime"] = "• Temps d'oratòria: ~{0} min",
                    ["SessionStats"] = "📈 Estadístiques de Sessió:",
                    ["WordsWrittenToday"] = "• Paraules escrites avui: {0:N0}",
                    ["WordsInSession"] = "• Paraules en aquesta sessió: {0:N0}",
                    ["NoDocumentSelected"] = "Cap Document Seleccionat",
                    ["NoDocumentSelectedDescription"] = "Si us plau, selecciona o crea un document abans de pujar-lo a GitHub.",
                    ["Connected"] = "Connectat",
                    ["NotConnected"] = "No Connectat",
                    ["LanguageChanged"] = "Idioma canviat a {0}",
                    ["LastModified"] = "Última modificació",
                    ["DocumentIndex"] = "Índex del Document",
                    ["WelcomeToJot"] = "Benvingut a Jot",
                    ["CreateFirstDocument"] = "Crea el teu primer document per començar",
                    ["CreateNewDocument"] = "Crear Nou Document",
                    ["GitHub"] = "GitHub",
                    ["ConnectGitHub"] = "Connectar GitHub",
                    ["UploadCurrent"] = "Pujar Actual",
                    ["Repositories"] = "Repositoris",
                    ["Disconnect"] = "Desconnectar",
                    ["ExpandGitHubOptions"] = "Expandir Opcions de GitHub",
                    ["DocumentTitle"] = "Títol del document...",
                    ["SearchDocuments"] = "Cercar documents...",
                    ["WordsCount"] = "Paraules: {0}",
                    ["CharactersCount"] = "Caràcters: {0}",
                    ["ReadingTimeCount"] = "Temps de lectura: {0} min",
                    ["LineColumn"] = "Línia {0}, Columna {1}",
                    ["ExportingToHtml"] = "Exportant a HTML...",
                    ["ToggleSidebar"] = "Alternar Barra Lateral",
                    ["QuickUploadToGitHub"] = "Pujada Ràpida a GitHub",
                    ["GitHubSettings"] = "Configuració de GitHub",
                    ["AIAssistant"] = "Assistent IA",
                    ["PythonCodeExecution"] = "Execució de Codi Python",
                    ["Close"] = "Tancar",
                    ["Send"] = "Enviar",
                    ["EnterYourQuestion"] = "Escriu la teva pregunta...",
                    ["SuggestedQuestions"] = "Preguntes suggerides:",
                    ["ExportDocument"] = "Exportar a HTML",
                    ["DeleteDocument"] = "Eliminar",
                    ["UploadDocument"] = "Pujar a GitHub",
                    ["ChatbotWelcome"] = "Hola! Sóc el teu assistent de documents. Pots preguntar-me sobre el contingut dels teus documents."
                },
                ["ast"] = new Dictionary<string, string>
                {
                    ["AppTitle"] = "Jot - Toma de Notes Moderna",
                    ["Documents"] = "Documentos",
                    ["NewDocument"] = "Documentu Nuevu",
                    ["SaveDocument"] = "Guardar Documentu",
                    ["DeleteDocument"] = "Desaniciar Documentu",
                    ["ExportToHtml"] = "Esportar a HTML",
                    ["UploadToGitHub"] = "Xubir a GitHub",
                    ["Search"] = "Guetar",
                    ["Language"] = "Idioma",
                    ["EditMode"] = "Editar",
                    ["PreviewMode"] = "Vista Previa",
                    ["SplitMode"] = "Dividir",
                    ["VoiceRecognition"] = "Reconocimientu de Voz",
                    ["Listening"] = "🎤 Escuchando... Fala agora",
                    ["VoiceRecognitionStopped"] = "🔇 Reconocimientu de voz paráu",
                    ["ReadingText"] = "🔊 Llevendo Testu",
                    ["PlayingTextAloud"] = "Reproduciendo testu en voz alta...",
                    ["Cancel"] = "Encaboxar",
                    ["Stop"] = "Parar",
                    ["OK"] = "Val",
                    ["FocusModeActivated"] = "🎯 Mou Concentración Activáu",
                    ["FocusModeDeactivated"] = "🎯 Mou Concentración Desactiváu",
                    ["FocusModeDescription"] = "Interfaz simplificada pa concentrase.\nPrimi F11 pa salir.",
                    ["FocusModeRestored"] = "Interfaz completa restaurada.",
                    ["FontSize"] = "📏 Tamañu de Lletra",
                    ["CurrentSize"] = "Tamañu actual: {0}pt",
                    ["PomodoroCompleted"] = "🍅 Pomodoro Completáu!",
                    ["PomodoroCompletedDescription"] = "¡Bon trabayu! Completasti una sesión de concentración de 25 minutos.\n\n¿Ye hora d'un descansu de 5 minutos?",
                    ["StartBreak"] = "Entamar Descansu",
                    ["ContinueWriting"] = "Siguir Escribiendo",
                    ["Close"] = "Zarrar",
                    ["AdvancedSearch"] = "Busca Avanzada",
                    ["SearchFor"] = "Guetar:",
                    ["SearchIn"] = "Guetar en:",
                    ["CurrentDocument"] = "Documentu Actual",
                    ["AllDocuments"] = "Tolos Documentos",
                    ["SelectedDocuments"] = "Documentos Escoyíos",
                    ["UseRegularExpressions"] = "Usar espresiones regulares",
                    ["WholeWordsOnly"] = "Namái pallabres completes",
                    ["FindAndReplace"] = "Guetar y Trocar",
                    ["Find"] = "Guetar:",
                    ["ReplaceWith"] = "Trocar con:",
                    ["CaseSensitive"] = "Distinguir mayúscules y minúscules",
                    ["ReplaceAll"] = "Trocar Too",
                    ["FindNext"] = "Guetar Siguiente",
                    ["SearchResult"] = "🔍 Resultáu de Busca",
                    ["FoundAtPosition"] = "Atopáu na posición {0}",
                    ["NotFound"] = "'{0}' non atopáu",
                    ["ReplacementsCompleted"] = "Realizáronse {0} trocamientos",
                    ["DocumentStatistics"] = "📊 Estadístiques del Documentu",
                    ["CompleteStatistics"] = "📊 Estadístiques Completes",
                    ["Content"] = "📝 Conteníu:",
                    ["Words"] = "• Pallabres: {0:N0}",
                    ["Characters"] = "• Carauteres: {0:N0}",
                    ["CharactersNoSpaces"] = "• Carauteres (ensin espacios): {0:N0}",
                    ["Lines"] = "• Llinies: {0:N0}",
                    ["Paragraphs"] = "• Párrafos: {0:N0}",
                    ["Sentences"] = "• Frases: {0:N0}",
                    ["AvgWordsPerSentence"] = "• Promediu pallabres/frase: {0}",
                    ["EstimatedTime"] = "⏱️ Tiempu Estimáu:",
                    ["ReadingTime"] = "• Tiempu de llectura: ~{0} min",
                    ["SpeakingTime"] = "• Tiempu d'oratoria: ~{0} min",
                    ["SessionStats"] = "📈 Estadístiques de Sesión:",
                    ["WordsWrittenToday"] = "• Pallabres escrites güei: {0:N0}",
                    ["WordsInSession"] = "• Pallabres nesta sesión: {0:N0}",
                    ["NoDocumentSelected"] = "Dengún Documentu Escoyíu",
                    ["NoDocumentSelectedDescription"] = "Por favor escueyi o cria un documentu enantes de xubilu a GitHub.",
                    ["Connected"] = "Coneutáu",
                    ["NotConnected"] = "Non Coneutáu",
                    ["LanguageChanged"] = "Idioma camudáu a {0}",
                    ["LastModified"] = "Última modificación",
                    ["DocumentIndex"] = "Índiz del Documentu",
                    ["WelcomeToJot"] = "Bienllegáu a Jot",
                    ["CreateFirstDocument"] = "Cria'l to primer documentu p'entamar",
                    ["CreateNewDocument"] = "Criar Documentu Nuevu",
                    ["GitHub"] = "GitHub",
                    ["ConnectGitHub"] = "Coneutar GitHub",
                    ["UploadCurrent"] = "Xubir Actual",
                    ["Repositories"] = "Repositorios",
                    ["Disconnect"] = "Desconeutar",
                    ["ExpandGitHubOptions"] = "Espander Opciones de GitHub",
                    ["DocumentTitle"] = "Títulu del documentu...",
                    ["SearchDocuments"] = "Guetar documentos...",
                    ["WordsCount"] = "Pallabres: {0}",
                    ["CharactersCount"] = "Carauteres: {0}",
                    ["ReadingTimeCount"] = "Tiempu de llectura: {0} min",
                    ["LineColumn"] = "Llinia {0}, Columna {1}",
                    ["ExportingToHtml"] = "Esportando a HTML...",
                    ["ToggleSidebar"] = "Alternar Barra Llateral",
                    ["QuickUploadToGitHub"] = "Xubida Rápida a GitHub",
                    ["GitHubSettings"] = "Configuración de GitHub",
                    ["AIAssistant"] = "Asistente IA",
                    ["PythonCodeExecution"] = "Execución de Códigu Python",
                    ["Close"] = "Zarrar",
                    ["Send"] = "Unviar",
                    ["EnterYourQuestion"] = "Escribi la to entruga...",
                    ["SuggestedQuestions"] = "Entrugues suxeríes:",
                    ["ExportDocument"] = "Esportar a HTML",
                    ["DeleteDocument"] = "Desaniciar",
                    ["UploadDocument"] = "Xubir a GitHub",
                    ["ChatbotWelcome"] = "¡Hola! Soi el to asistente de documentos. Pues entrugame sobro'l conteníu de los tos documentos."
                }
            };
        }

        public void SetLanguage(string languageCode)
        {
            try
            {
                if (!_supportedLanguages.ContainsKey(languageCode))
                {
                    languageCode = "en";
                }

                CurrentLanguage = languageCode;

                // Notificar cambio global para forzar actualización de todas las propiedades
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

                // Notificar cambios específicos
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguageName)));

                System.Diagnostics.Debug.WriteLine($"Language changed to: {languageCode} ({CurrentLanguageName})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting language: {ex.Message}");
            }
        }

        public string GetString(string key)
        {
            try
            {
                if (_localizedStrings != null && 
                    _localizedStrings.TryGetValue(CurrentLanguage, out var languageDict) &&
                    languageDict.TryGetValue(key, out var value))
                {
                    return value;
                }

                // Fallback a inglés si no se encuentra en el idioma actual
                if (_localizedStrings != null && 
                    _localizedStrings.TryGetValue("en", out var englishDict) &&
                    englishDict.TryGetValue(key, out var englishValue))
                {
                    return englishValue;
                }

                return key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting string for key '{key}': {ex.Message}");
                return key;
            }
        }

        public string GetString(string key, params object[] args)
        {
            try
            {
                var format = GetString(key);
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error formatting string for key '{key}': {ex.Message}");
                return key;
            }
        }
    }
}