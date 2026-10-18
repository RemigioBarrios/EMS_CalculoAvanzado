using System;
using System.Collections.Generic;

namespace CalculusApp.Services
{
    public enum AppLanguage
    {
        English,
        Spanish,
        Portuguese
    }

    public static class LocalizationService
    {
        public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Spanish; // Default or English

        public static event Action? LanguageChanged;

        public static void SetLanguage(AppLanguage language)
        {
            if (CurrentLanguage != language)
            {
                CurrentLanguage = language;
                LanguageChanged?.Invoke();
            }
        }

        private static readonly Dictionary<string, (string en, string es, string pt)> Strings = new()
        {
            // Menus
            { "menu_file", ("_File", "_Archivo", "_Arquivo") },
            { "menu_new", ("_New Assignment", "_Nuevo Examen", "_Novo Teste") },
            { "menu_open", ("_Open...", "_Abrir...", "_Abrir...") },
            { "menu_save", ("_Save", "_Guardar", "_Salvar") },
            { "menu_print", ("_Print...", "_Imprimir...", "_Imprimir...") },
            { "menu_exit", ("E_xit", "S_alir", "S_air") },
            { "menu_edit", ("_Edit", "_Editar", "_Editar") },
            { "menu_undo", ("_Undo", "_Deshacer", "_Desfazer") },
            { "menu_redo", ("_Redo", "_Rehacer", "_Refazer") },
            { "menu_regen_all", ("Regenerate _All Questions", "Regenerar _Todas las Preguntas", "Regenerar _Todas as Questões") },
            { "menu_scramble_q", ("_Scramble Questions", "_Mezclar Preguntas", "_Embaralhar Questões") },
            { "menu_scramble_a", ("Scramble Choice _Answers", "Mezclar _Respuestas de Opciones", "Embaralhar _Respostas das Opções") },
            { "menu_view", ("_View", "_Ver", "_Exibir") },
            { "menu_show_answers", ("Show/Hide _Answers", "Mostrar/Ocultar _Respuestas", "Mostrar/Ocultar _Respostas") },
            { "menu_punch_holes", ("Show/Hide _Punch Holes", "Mostrar/Ocultar _Perforaciones", "Mostrar/Ocultar _Furos") },
            { "menu_theme", ("_Theme", "_Tema", "_Tema") },
            { "menu_theme_light", ("☀️ Light Theme", "☀️ Tema Claro", "☀️ Tema Claro") },
            { "menu_theme_dark", ("🌙 Dark Theme", "🌙 Tema Oscuro", "🌙 Tema Escuro") },
            { "menu_language", ("_Language", "_Idioma", "_Idioma") },
            { "menu_modify", ("_Modify", "_Modificar", "_Modificar") },
            { "menu_all_mc", ("Change All to _Multiple Choice", "Cambiar Todo a Opción _Múltiple", "Mudar Tudo para Múltipla _Escolha") },
            { "menu_all_fr", ("Change All to _Free Response", "Cambiar Todo a Respuesta _Libre", "Mudar Tudo para Resposta _Livre") },
            { "menu_shorten", ("Shorten Assignment", "Acortar Examen", "Encurtar Atividade") },
            { "menu_lengthen", ("Lengthen Assignment", "Alargar Examen", "Alongar Atividade") },
            { "menu_distribute_space", ("Distribute Space at Bottom", "Distribuir Espacio al Final", "Distribuir Espaço no Fim") },
            { "menu_utilities", ("_Utilities", "_Utilidades", "_Utilitários") },
            { "menu_help", ("_Help", "A_yuda", "A_juda") },
            { "menu_about", ("About Infinite Calculus...", "Acerca de Infinite Calculus...", "Sobre o Infinite Calculus...") },

            // Toolbar
            { "tb_new", ("New", "Nuevo", "Novo") },
            { "tb_open", ("Open", "Abrir", "Abrir") },
            { "tb_save", ("Save", "Guardar", "Salvar") },
            { "tb_print", ("Print", "Imprimir", "Imprimir") },
            { "tb_heading", ("Heading", "Encabezado", "Cabeçalho") },
            { "tb_show_answers", ("Show Answers", "Respuestas", "Respostas") },
            { "tb_scramble", ("Scramble", "Mezclar", "Embaralhar") },
            { "tb_regenerate_all", ("Regenerate All", "Regenerar Todo", "Regenerar Tudo") },
            { "tb_all_mc", ("All MC", "Todo MC", "Tudo ME") },
            { "tb_all_fr", ("All FR", "Todo RL", "Tudo RL") },
            { "tb_punch_holes", ("Punch Holes", "Perforaciones", "Furos") },
            { "tb_theme_light", ("Light", "Claro", "Claro") },
            { "tb_theme_dark", ("Dark", "Oscuro", "Escuro") },
            { "tb_theme_toggle", ("Toggle Theme", "Cambiar Tema", "Alternar Tema") },
            { "tb_zoom", ("Zoom:", "Zoom:", "Zoom:") },
            { "tb_cols", ("Cols:", "Cols:", "Colunas:") },

            // Sidebar
            { "filter_topics_placeholder", ("🔍 Filter topics...", "🔍 Filtrar temas...", "🔍 Filtrar tópicos...") },
            { "current_question_sets", ("Current Question Sets:", "Conjuntos de Preguntas Activos:", "Conjuntos de Questões Ativos:") },
            { "pages_stat", ("{0:0.00} pages", "{0:0.00} páginas", "{0:0.00} páginas") },
            { "questions_stat", ("{0} questions", "{0} preguntas", "{0} questões") },
            { "question_singular", ("question", "pregunta", "questão") },
            { "questions_plural", ("questions", "preguntas", "questões") },

            // Worksheet Layout
            { "ws_name", ("Name___________________________________", "Nombre___________________________________", "Nome___________________________________") },
            { "ws_date_period", ("Date________________   Period____", "Fecha________________   Período____", "Data________________   Período____") },
            { "ws_assignment", ("Assignment", "Guía de Ejercicios", "Lista de Exercícios") },
            { "ws_answers_to", ("Answers to {0}", "Respuestas de {0}", "Respostas de {0}") },
            { "ws_ans_label", ("Ans:", "Resp:", "Resp:") },
            { "ws_footer", ("Worksheet by {0} Studio. All rights reserved.", "Hoja de ejercicios de {0} Studio. Todos los derechos reservados.", "Folha de exercícios de {0} Studio. Todos os direitos reservados.") },
            { "menu_export_pdf", ("_Export to PDF...", "_Exportar a PDF...", "_Exportar para PDF...") },
            { "menu_preview_pdf", ("PDF _Preview...", "_Vista Previa de PDF...", "_Visualizar PDF...") },
            { "tb_export_pdf", ("PDF", "PDF", "PDF") },
            { "tb_preview_pdf", ("Preview", "Vista Previa", "Visualizar") },
            { "pdf_preview_title", ("PDF Document Preview - Infinite Calculus", "Vista Previa del Documento PDF - Infinite Calculus", "Pré-visualização de Documento PDF - Infinite Calculus") },
            { "pdf_page_n_of_m", ("Page {0} of {1}", "Página {0} de {1}", "Página {0} de {1}") },
            { "pdf_fit_width", ("Fit Width", "Ajustar al Ancho", "Ajustar à Largura") },
            { "pdf_fit_page", ("Fit Page", "Página Completa", "Página Inteira") },
            { "pdf_thumbnails", ("Thumbnails", "Miniaturas", "Miniaturas") },
            { "pdf_export_success", ("PDF successfully exported to:\n{0}", "¡Documento PDF exportado exitosamente en:\n{0}!", "PDF exportado com sucesso em:\n{0}!") }
        };

        public static string Get(string key, params object[] args)
        {
            if (Strings.TryGetValue(key, out var tuple))
            {
                string template = CurrentLanguage switch
                {
                    AppLanguage.English => tuple.en,
                    AppLanguage.Spanish => tuple.es,
                    AppLanguage.Portuguese => tuple.pt,
                    _ => tuple.en
                };
                return args.Length > 0 ? string.Format(template, args) : template;
            }
            return key;
        }
    }
}
