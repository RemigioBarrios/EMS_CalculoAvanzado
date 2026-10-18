using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CalculusApp.Controls;
using CalculusApp.Generators;
using CalculusApp.Models;
using CalculusApp.Services;

namespace CalculusApp.Dialogs
{
    public partial class TopicOptionsDialog : Window
    {
        private readonly TopicDefinition _topic;
        private readonly Random _rng = new();
        private bool _isInitialized = false;

        public TopicGeneratorParameters Parameters { get; } = new();
        private List<Question> _currentPreviewQuestions = new();
        public event Action<int, QuestionType, TopicGeneratorParameters, List<Question>>? QuestionsAdded;

        public TopicOptionsDialog(TopicDefinition topic, int initialCount = 4, QuestionType initialType = QuestionType.FreeResponse, TopicGeneratorParameters? existingParams = null)
        {
            InitializeComponent();
            _topic = topic ?? throw new ArgumentNullException(nameof(topic));

            if (existingParams != null)
            {
                Parameters = existingParams;
            }
            Parameters.QuestionCount = initialCount;
            Parameters.QuestionType = initialType;

            NumQuestionsBox.Text = initialCount.ToString();

            ApplyThemeAndLocalization();
            ConfigureAdaptiveComplexity();
            SyncUIFromParameters();

            _isInitialized = true;
            UpdateSamples();
        }

        private void ApplyThemeAndLocalization()
        {
            var lang = LocalizationService.CurrentLanguage;

            // Title format: Category - Topic
            Title = $"{_topic.Category} - {_topic.Title}";

            // Preview direction header
            PreviewDirectionHeader.Text = _topic.DefaultDirections;

            switch (lang)
            {
                case AppLanguage.Spanish:
                    NumQuestionsLabel.Text = "Número de preguntas:";
                    OkButton.Content = "Aceptar";
                    AddContinueButton.Content = "Añadir y Continuar";
                    CancelButton.Content = "Cancelar";
                    MoreSamplesBtn.Content = "Más muestras";
                    PresetGroupBox.Header = "Opciones preestablecidas";
                    EasyBtn.Content = "Fácil";
                    MediumBtn.Content = "Medio";
                    HardBtn.Content = "Difícil";
                    QuestionTypeGroupBox.Header = "Tipo de ejercicio";
                    FreeResponseRadio.Content = "Respuesta libre";
                    MultipleChoiceRadio.Content = "Opción múltiple";
                    OptionsGroupBox.Header = "Opciones";
                    NotationLabel.Text = "Notación: ";
                    NotationEitherRadio.Content = "Cualquiera";
                    VaryLettersCheck.Content = "Variar letras para funciones y variables";
                    BlankGraphCheck.Content = "Proporcionar gráfica en blanco";
                    FunctionGraphCheck.Content = "Proporcionar gráfica de la función";
                    ComplexityGroupBox.Header = "Complejidad";
                    FunctionsSubheader.Text = "Funciones:";
                    break;

                case AppLanguage.Portuguese:
                    NumQuestionsLabel.Text = "Quantidade de questões:";
                    OkButton.Content = "OK";
                    AddContinueButton.Content = "Adicionar e Continuar";
                    CancelButton.Content = "Cancelar";
                    MoreSamplesBtn.Content = "Mais amostras";
                    PresetGroupBox.Header = "Predefinições";
                    EasyBtn.Content = "Fácil";
                    MediumBtn.Content = "Médio";
                    HardBtn.Content = "Difícil";
                    QuestionTypeGroupBox.Header = "Tipo de questão";
                    FreeResponseRadio.Content = "Resposta livre";
                    MultipleChoiceRadio.Content = "Múltipla escolha";
                    OptionsGroupBox.Header = "Opções";
                    NotationLabel.Text = "Notação: ";
                    NotationEitherRadio.Content = "Qualquer";
                    VaryLettersCheck.Content = "Variar letras para funções e variáveis";
                    BlankGraphCheck.Content = "Fornecer gráfico em branco";
                    FunctionGraphCheck.Content = "Fornecer gráfico da função";
                    ComplexityGroupBox.Header = "Complexidade";
                    FunctionsSubheader.Text = "Funções:";
                    break;

                default:
                    NumQuestionsLabel.Text = "Number of questions:";
                    OkButton.Content = "OK";
                    AddContinueButton.Content = "Add & Continue";
                    CancelButton.Content = "Cancel";
                    MoreSamplesBtn.Content = "More samples";
                    PresetGroupBox.Header = "Preset choices";
                    EasyBtn.Content = "Easy";
                    MediumBtn.Content = "Medium";
                    HardBtn.Content = "Hard";
                    QuestionTypeGroupBox.Header = "Question type";
                    FreeResponseRadio.Content = "Free response";
                    MultipleChoiceRadio.Content = "Multiple choice";
                    OptionsGroupBox.Header = "Options";
                    NotationLabel.Text = "Notation: ";
                    NotationEitherRadio.Content = "Either";
                    VaryLettersCheck.Content = "Vary letters for functions and variables";
                    BlankGraphCheck.Content = "Provide a blank graph";
                    FunctionGraphCheck.Content = "Provide a graph of the function";
                    ComplexityGroupBox.Header = "Complexity";
                    FunctionsSubheader.Text = "Functions:";
                    break;
            }
        }

        private string GetTopicArchetype()
        {
            return _topic.TopicId switch
            {
                "deriv_avg_rates" => "deriv_avg_rates",
                "deriv_definition" => "deriv_definition",
                "deriv_instant_rates" or "app_tangent" => "deriv_instant_rates",
                "deriv_power" => "deriv_power",
                "deriv_higher_order" => "deriv_higher_order",
                "deriv_product" => "deriv_product",
                "deriv_quotient" => "deriv_quotient",
                "deriv_chain" => "deriv_chain",
                "deriv_trig" or "deriv_inv_trig" => "deriv_trig",
                "deriv_exp_log_natural" or "deriv_exp_log_other" or "deriv_logarithmic" => "deriv_exp_log",
                "deriv_implicit" => "deriv_implicit",
                "deriv_inverse_func" or "deriv_tables" => "deriv_power",
                "app_abs_extrema" => "app_abs_extrema",
                "app_rel_extrema" => "app_rel_extrema",
                "app_concavity" or "app_increase_decrease" => "app_concavity",
                "app_mvt" or "app_rolle" => "app_mvt",
                "app_optimization" => "app_optimization",
                "app_motion" or "app_int_motion_revisited" => "app_motion",
                "app_related_rates" => "app_related_rates",
                "app_differentials" => "app_differentials",
                "app_newton" => "app_newton",
                "app_lhopital" => "app_lhopital",
                "app_curve_sketching" or "app_graph_comparison" => "app_concavity",
                "limits_holes" => "limits_holes",
                "limits_jumps" => "limits_jumps",
                "limits_essential" or "limits_infinity" => "limits_infinity",
                "limits_direct" or "continuity_classify" or "app_limits_deriv_form" => "limits_direct",
                "indef_power" or "indef_log_exp" or "indef_trig" or "indef_inv_trig" => "indef_basic",
                "indef_subs_power" or "indef_subs_log_exp" or "indef_subs_trig" or "indef_subs_inv_trig" => "indef_subs",
                "indef_parts" => "indef_parts",
                "def_approx_area" or "def_limits_sums" or "def_riemann_tables" => "def_riemann",
                "def_ftc1" or "def_ftc2" => "def_ftc",
                "def_subs_change_vars" or "def_mvt_integrals" => "def_subs",
                "app_int_area_curve" or "app_int_area_between" => "app_int_area",
                "app_int_volume_disks" or "app_int_volume_shells" or "app_int_cross_sections" => "app_int_volume",
                "diff_eq_slope_fields" => "diff_eq_slope_fields",
                "diff_eq_separable" or "diff_eq_intro" or "diff_eq_growth_decay" => "diff_eq_separable",
                _ => _topic.CategoryId
            };
        }

        private void ConfigureAdaptiveComplexity()
        {
            string arch = GetTopicArchetype();
            var lang = LocalizationService.CurrentLanguage;

            // Reset all to visible by default
            ComplexityRadio1.Visibility = Visibility.Visible;
            ComplexityRadio2.Visibility = Visibility.Visible;
            ComplexityRadio3.Visibility = Visibility.Visible;
            ComplexityRadio4.Visibility = Visibility.Visible;
            ComplexityRadio5.Visibility = Visibility.Visible;

            FunctionsSubheader.Visibility = Visibility.Visible;
            FuncCheck1.Visibility = Visibility.Visible;
            FuncCheck2.Visibility = Visibility.Visible;
            FuncCheck3.Visibility = Visibility.Visible;
            FuncCheck4.Visibility = Visibility.Visible;
            FuncCheck5.Visibility = Visibility.Visible;
            FuncCheck6.Visibility = Visibility.Visible;
            FuncCheck7.Visibility = Visibility.Visible;
            FuncCheck8.Visibility = Visibility.Collapsed;

            if (arch == "deriv_avg_rates")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Para cada problema, calcular:",
                    AppLanguage.Portuguese => "Para cada problema, calcular:",
                    _ => "For each problem, calculate:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Tasa de cambio promedio en el intervalo dado", AppLanguage.Portuguese => "Taxa de variação média no intervalo dado", _ => "The average rate of change over the given interval" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Ecuación de la recta secante entre los puntos dados", AppLanguage.Portuguese => "Equação da reta secante entre os pontos dados", _ => "The secant line between the given points" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Ambos (Tasa de cambio y recta secante)", AppLanguage.Portuguese => "Ambos (Taxa de variação e reta secante)", _ => "Both (Average rate & secant line)" };
                ComplexityRadio4.Visibility = Visibility.Collapsed;
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Exponenciales", AppLanguage.Portuguese => "Exponenciais", _ => "Exponential" };
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "deriv_definition")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Definición del límite de la derivada a utilizar:",
                    AppLanguage.Portuguese => "Definição do limite da derivada a utilizar:",
                    _ => "Limit definition format to use:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Forma de incremento: lim(h->0) [f(x+h) - f(x)] / h", AppLanguage.Portuguese => "Forma de incremento: lim(h->0) [f(x+h) - f(x)] / h", _ => "Increment form: lim(h->0) [f(x+h) - f(x)] / h" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Forma alternativa: lim(x->a) [f(x) - f(a)] / (x - a)", AppLanguage.Portuguese => "Forma alternativa: lim(x->a) [f(x) - f(a)] / (x - a)", _ => "Alternative form: lim(x->a) [f(x) - f(a)] / (x - a)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Either / Any" };
                ComplexityRadio4.Visibility = Visibility.Collapsed;
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios (cuadrática, cúbica)", AppLanguage.Portuguese => "Polinômios (quadrática, cúbica)", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Racionales simples", AppLanguage.Portuguese => "Racionais simples", _ => "Simple rational" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck4.Visibility = Visibility.Collapsed;
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "deriv_instant_rates")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Para cada problema, calcular:",
                    AppLanguage.Portuguese => "Para cada problema, calcular:",
                    _ => "For each problem, calculate:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Tasa de cambio instantánea en el punto", AppLanguage.Portuguese => "Taxa de variação instantânea no ponto", _ => "Instantaneous rate of change at point" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Pendiente de la recta tangente", AppLanguage.Portuguese => "Inclinação da reta tangente", _ => "Slope of the tangent line" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Ecuación de la recta tangente", AppLanguage.Portuguese => "Equação da reta tangente", _ => "Equation of the tangent line" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Ecuación de la recta normal", AppLanguage.Portuguese => "Equação da reta normal", _ => "Equation of the normal line" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Puntos de tangencia horizontal", AppLanguage.Portuguese => "Pontos de tangência horizontal", _ => "Points of horizontal tangents" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Exponenciales", AppLanguage.Portuguese => "Exponenciais", _ => "Exponential" };
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "deriv_power")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Términos y complejidad de la potencia:",
                    AppLanguage.Portuguese => "Termos e complexidade da potência:",
                    _ => "Terms and power complexity:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Monomios (1 término)", AppLanguage.Portuguese => "Monômios (1 termo)", _ => "Monomials (1 term)" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Binomios (2 términos)", AppLanguage.Portuguese => "Binômios (2 termos)", _ => "Binomials (2 terms)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Trinomios (3 términos)", AppLanguage.Portuguese => "Trinômios (3 termos)", _ => "Trinomials (3 terms)" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Polinomios generales", AppLanguage.Portuguese => "Polinômios gerais", _ => "General polynomials" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Exponentes enteros positivos", AppLanguage.Portuguese => "Expoentes inteiros positivos", _ => "Positive integer exponents" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Exponentes negativos", AppLanguage.Portuguese => "Expoentes negativos", _ => "Negative exponents" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Exponentes fraccionarios (raíces)", AppLanguage.Portuguese => "Expoentes fracionários (raízes)", _ => "Fractional exponents (roots)" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Coeficientes fraccionarios", AppLanguage.Portuguese => "Coeficientes fracionários", _ => "Fractional coefficients" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "deriv_higher_order")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Orden de la derivada requerida:",
                    AppLanguage.Portuguese => "Ordem da derivada requerida:",
                    _ => "Required derivative order:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Segunda derivada f''(x)", AppLanguage.Portuguese => "Segunda derivada f''(x)", _ => "Second derivative f''(x)" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Tercera derivada f'''(x)", AppLanguage.Portuguese => "Terceira derivada f'''(x)", _ => "Third derivative f'''(x)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Cuarta derivada f^(4)(x)", AppLanguage.Portuguese => "Quarta derivada f^(4)(x)", _ => "Fourth derivative f^(4)(x)" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cualquier orden (2° a 4°)", AppLanguage.Portuguese => "Qualquer ordem (2° a 4°)", _ => "Any order (2nd to 4th)" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Exponenciales", AppLanguage.Portuguese => "Exponenciais", _ => "Exponential" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Racionales simples", AppLanguage.Portuguese => "Racionais simples", _ => "Simple rational" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "deriv_product" || arch == "deriv_quotient")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Estructura de la función:",
                    AppLanguage.Portuguese => "Estrutura da função:",
                    _ => "Function structure:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Dos factores / Polinomio entre polinomio", AppLanguage.Portuguese => "Dois fatores / Polinômio por polinômio", _ => "Two factors / Poly / Poly" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Con funciones trigonométricas", AppLanguage.Portuguese => "Com funções trigonométricas", _ => "With trigonometric" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Con exponenciales o logaritmos", AppLanguage.Portuguese => "Com exponenciais ou logaritmos", _ => "With exponentials or logs" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cualquier combinación", AppLanguage.Portuguese => "Qualquer combinação", _ => "Any combination" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Exponenciales", AppLanguage.Portuguese => "Exponenciais", _ => "Exponential" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Logaritmos", AppLanguage.Portuguese => "Logaritmos", _ => "Logarithmic" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "deriv_chain")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Tipo y profundidad de regla de la cadena:",
                    AppLanguage.Portuguese => "Tipo e profundidade da regra da cadeia:",
                    _ => "Chain rule depth and type:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Potencia general (u(x))^n", AppLanguage.Portuguese => "Potência geral (u(x))^n", _ => "General power rule (u(x))^n" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Trigonométrica compuesta sin(u(x))", AppLanguage.Portuguese => "Trigonométrica composta sin(u(x))", _ => "Composite trigonometric sin(u(x))" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Exponencial compuesta e^(u(x))", AppLanguage.Portuguese => "Exponencial composta e^(u(x))", _ => "Composite exponential e^(u(x))" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cadena anidada dos veces", AppLanguage.Portuguese => "Cadeia aninhada duas vezes", _ => "Chain rule nested twice" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios internos", AppLanguage.Portuguese => "Polinômios internos", _ => "Inner polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Exponenciales", AppLanguage.Portuguese => "Exponenciais", _ => "Exponential" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Logaritmos", AppLanguage.Portuguese => "Logaritmos", _ => "Logarithmic" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Trigonométricas inversas", AppLanguage.Portuguese => "Trigonométricas inversas", _ => "Inverse trig" };
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "deriv_implicit")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Estructura de la ecuación implícita:",
                    AppLanguage.Portuguese => "Estrutura da equação implícita:",
                    _ => "Implicit equation structure:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Términos lineales en y", AppLanguage.Portuguese => "Termos lineares em y", _ => "Linear terms in y" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Potencias de y (y^2, y^3)", AppLanguage.Portuguese => "Potências de y (y^2, y^3)", _ => "Powers of y (y^2, y^3)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Términos producto cruzado xy", AppLanguage.Portuguese => "Termos produto cruzado xy", _ => "Cross product terms xy" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Ecuaciones implícitas generales", AppLanguage.Portuguese => "Equações implícitas gerais", _ => "General implicit equations" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Hallar dy/dx", AppLanguage.Portuguese => "Encontrar dy/dx", _ => "Find dy/dx" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Evaluar en punto dado", AppLanguage.Portuguese => "Avaliar no ponto dado", _ => "Evaluate at point" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Hallar recta tangente", AppLanguage.Portuguese => "Encontrar reta tangente", _ => "Find tangent line" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Hallar segunda derivada d²y/dx²", AppLanguage.Portuguese => "Encontrar d²y/dx²", _ => "Find d²y/dx²" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_abs_extrema")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Para cada problema, halle todos los puntos extremos sobre:",
                    AppLanguage.Portuguese => "Para cada problema, encontre todos os pontos extremos sobre:",
                    _ => "For each problem, find all points of absolute extrema over:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Un intervalo cerrado", AppLanguage.Portuguese => "Um intervalo fechado", _ => "A closed interval" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Un intervalo abierto", AppLanguage.Portuguese => "Um intervalo aberto", _ => "An open interval" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Un intervalo semiabierto", AppLanguage.Portuguese => "Um intervalo semiaberto", _ => "A half open interval" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Un intervalo con infinito", AppLanguage.Portuguese => "Um intervalo envolvendo infinito", _ => "An interval involving infinity" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Cuadrática", AppLanguage.Portuguese => "Quadrática", _ => "Quadratic" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Cúbica", AppLanguage.Portuguese => "Cúbica", _ => "Cubic" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Cuártica", AppLanguage.Portuguese => "Quártica", _ => "Quartic" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Racional", AppLanguage.Portuguese => "Racional", _ => "Rational" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Exponente racional, fácil", AppLanguage.Portuguese => "Expoente racional, fácil", _ => "Rational exponent, easy" };
                FuncCheck6.Content = lang switch { AppLanguage.Spanish => "Exponente racional, difícil", AppLanguage.Portuguese => "Expoente racional, difícil", _ => "Rational exponent, hard" };
                FuncCheck7.Content = lang switch { AppLanguage.Spanish => "Trigonométrica simple", AppLanguage.Portuguese => "Trigonométrica simples", _ => "Simple trigonometric" };
            }
            else if (arch == "app_rel_extrema")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Para cada problema, halle los extremos relativos:",
                    AppLanguage.Portuguese => "Para cada problema, encontre os extremos relativos:",
                    _ => "For each problem, find all points of relative extrema:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Criterio de la primera derivada", AppLanguage.Portuguese => "Critério da primeira derivada", _ => "First derivative test" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Criterio de la segunda derivada", AppLanguage.Portuguese => "Critério da segunda derivada", _ => "Second derivative test" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Cualquier método", AppLanguage.Portuguese => "Qualquer método", _ => "Any method" };
                ComplexityRadio4.Visibility = Visibility.Collapsed;
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Cuadrática", AppLanguage.Portuguese => "Quadrática", _ => "Quadratic" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Cúbica", AppLanguage.Portuguese => "Cúbica", _ => "Cubic" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Cuártica", AppLanguage.Portuguese => "Quártica", _ => "Quartic" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Racional", AppLanguage.Portuguese => "Racional", _ => "Rational" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Radical", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck6.Content = lang switch { AppLanguage.Spanish => "Trigonométrica", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_concavity")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Determinar intervalos y puntos:",
                    AppLanguage.Portuguese => "Determinar intervalos e pontos:",
                    _ => "Determine intervals and points:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Solo intervalos de concavidad / crecimiento", AppLanguage.Portuguese => "Apenas intervalos de concavidade / crescimento", _ => "Intervals only" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Solo puntos de inflexión / extremos", AppLanguage.Portuguese => "Apenas pontos de inflexão / extremos", _ => "Inflection / critical points only" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Ambos (intervalos y puntos)", AppLanguage.Portuguese => "Ambos (intervalos e pontos)", _ => "Both (intervals and points)" };
                ComplexityRadio4.Visibility = Visibility.Collapsed;
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Cúbica", AppLanguage.Portuguese => "Cúbica", _ => "Cubic" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Cuártica", AppLanguage.Portuguese => "Quártica", _ => "Quartic" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Racional", AppLanguage.Portuguese => "Racional", _ => "Rational" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Radical", AppLanguage.Portuguese => "Radical", _ => "Radical" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Trigonométrica", AppLanguage.Portuguese => "Trigonométrica", _ => "Trigonometric" };
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_mvt")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Para cada problema, en el intervalo dado:",
                    AppLanguage.Portuguese => "Para cada problema, no intervalo dado:",
                    _ => "For each problem, over the given interval:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Verificar hipótesis y hallar valores c", AppLanguage.Portuguese => "Verificar hipótese e encontrar c", _ => "Verify hypothesis & find c" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Solo hallar los valores de c", AppLanguage.Portuguese => "Apenas encontrar os valores de c", _ => "Find values of c only" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Problemas donde no siempre aplica", AppLanguage.Portuguese => "Problemas onde nem sempre se aplica", _ => "Include problems where it does not apply" };
                ComplexityRadio4.Visibility = Visibility.Collapsed;
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_optimization")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Tipo de problema de optimización:",
                    AppLanguage.Portuguese => "Tipo de problema de otimização:",
                    _ => "Optimization problem context:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Números (sumas y productos extremos)", AppLanguage.Portuguese => "Números (somas e produtos extremos)", _ => "Numbers (sums & products)" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Áreas y perímetros (rectángulos, cercas)", AppLanguage.Portuguese => "Áreas e perímetros (retângulos, cercas)", _ => "Area and perimeter (fences, pens)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Volúmenes y superficies (cajas, latas)", AppLanguage.Portuguese => "Volumes e superfícies (caixas, latas)", _ => "Volume & surface area (boxes, cans)" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Distancia mínima a una curva", AppLanguage.Portuguese => "Distância mínima a uma curva", _ => "Minimum distance to curve" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Respuestas exactas", AppLanguage.Portuguese => "Respostas exatas", _ => "Exact answers" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Aproximaciones decimales", AppLanguage.Portuguese => "Aproximações decimais", _ => "Decimal approximations" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Pedir dimensiones óptimas", AppLanguage.Portuguese => "Pedir dimensões ótimas", _ => "Ask for dimensions" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Pedir valor óptimo extremo", AppLanguage.Portuguese => "Pedir valor ótimo extremo", _ => "Ask for optimal value" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_lhopital")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Forma indeterminada de L'Hôpital:",
                    AppLanguage.Portuguese => "Forma indeterminada de L'Hôpital:",
                    _ => "L'Hopital indeterminate form:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Forma 0/0", AppLanguage.Portuguese => "Forma 0/0", _ => "Form 0/0" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Forma infinito / infinito", AppLanguage.Portuguese => "Forma infinito / infinito", _ => "Form infinity / infinity" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Requiere L'Hôpital dos veces", AppLanguage.Portuguese => "Requer L'Hôpital duas vezes", _ => "Requires L'Hopital twice" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Exponenciales e^x", AppLanguage.Portuguese => "Exponenciais e^x", _ => "Exponential e^x" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Logaritmos ln(x)", AppLanguage.Portuguese => "Logaritmos ln(x)", _ => "Logarithms ln(x)" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch.StartsWith("limits"))
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Evaluar límites aplicando:",
                    AppLanguage.Portuguese => "Avaliar limites aplicando:",
                    _ => "Evaluate limits over:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Valores estándar finitos", AppLanguage.Portuguese => "Valores padrão finitos", _ => "Standard finite values" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Indeterminación 0/0 (factorización)", AppLanguage.Portuguese => "Indeterminação 0/0 (fatoração)", _ => "Indeterminate forms (0/0 factoring)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Límites laterales y saltos", AppLanguage.Portuguese => "Limites laterais e saltos", _ => "One-sided limits at kinks" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "En asíntotas verticales / infinito", AppLanguage.Portuguese => "Em assíntotas verticais / infinito", _ => "Limits at vertical asymptotes / infinity" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any limit type" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Valor absoluto", AppLanguage.Portuguese => "Valor absoluto", _ => "Absolute value" };
                FuncCheck6.Content = lang switch { AppLanguage.Spanish => "Exponenciales", AppLanguage.Portuguese => "Exponenciais", _ => "Exponential" };
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_motion")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Para cada problema de movimiento rectilíneo, calcular:",
                    AppLanguage.Portuguese => "Para cada problema de movimento retilíneo, calcular:",
                    _ => "For each rectilinear motion problem, calculate:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Desplazamiento y distancia total recorrida", AppLanguage.Portuguese => "Deslocamento e distância total percorrida", _ => "Displacement and total distance traveled" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Velocidad y aceleración en el instante dado", AppLanguage.Portuguese => "Velocidade e aceleração no instante dado", _ => "Velocity and acceleration at given time" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Instantes donde cambia de dirección (v(t) = 0)", AppLanguage.Portuguese => "Instantes onde muda de direção (v(t) = 0)", _ => "Times when particle changes direction" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Intervalos donde acelera o frena", AppLanguage.Portuguese => "Intervalos onde acelera ou desacelera", _ => "Intervals speeding up or slowing down" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Función posición s(t)", AppLanguage.Portuguese => "Função posição s(t)", _ => "Position function s(t)" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Función velocidad v(t)", AppLanguage.Portuguese => "Função velocidade v(t)", _ => "Velocity function v(t)" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Función aceleración a(t)", AppLanguage.Portuguese => "Função aceleração a(t)", _ => "Acceleration function a(t)" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Gráfica del movimiento", AppLanguage.Portuguese => "Gráfico do movimento", _ => "Motion graph" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_differentials")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Aproximación lineal y cálculo con diferenciales:",
                    AppLanguage.Portuguese => "Aproximação linear e cálculo com diferenciais:",
                    _ => "Linear approximation and differentials:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Hallar el diferencial dy = f'(x) dx", AppLanguage.Portuguese => "Encontrar o diferencial dy = f'(x) dx", _ => "Find differential dy = f'(x) dx" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Aproximar el cambio Δy ≈ dy", AppLanguage.Portuguese => "Aproximar a variação Δy ≈ dy", _ => "Approximate change Δy ≈ dy" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Aproximar valor numérico f(x0 + Δx)", AppLanguage.Portuguese => "Aproximar valor numérico f(x0 + Δx)", _ => "Approximate value f(x0 + Δx)" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Calcular error relativo y porcentual", AppLanguage.Portuguese => "Calcular erro relativo e percentual", _ => "Relative and percentage error" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "indef_parts")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Configuración de integración por partes:",
                    AppLanguage.Portuguese => "Configuração de integração por partes:",
                    _ => "Integration by parts configuration:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Una aplicación de u·v - ∫v du", AppLanguage.Portuguese => "Uma aplicação de u·v - ∫v du", _ => "One application of u*v - integral(v du)" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Dos aplicaciones sucesivas", AppLanguage.Portuguese => "Duas aplicações sucessivas", _ => "Two successive applications" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Proporcionar u y dv al estudiante", AppLanguage.Portuguese => "Fornecer u e dv ao estudante", _ => "Provide u and dv to student" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomio × Trigonométrica", AppLanguage.Portuguese => "Polinômio × Trigonométrica", _ => "Polynomial × Trig" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Polinomio × Exponencial", AppLanguage.Portuguese => "Polinômio × Exponencial", _ => "Polynomial × Exponential" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Polinomio × Logarítmica", AppLanguage.Portuguese => "Polinômio × Logarítmica", _ => "Polynomial × Logarithmic" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Exponencial × Trigonométrica", AppLanguage.Portuguese => "Exponencial × Trigonométrica", _ => "Exponential × Trig" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "indef_subs" || arch == "def_subs")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Tipo de sustitución (cambio de variable u):",
                    AppLanguage.Portuguese => "Tipo de substituição (mudança de variável u):",
                    _ => "U-substitution type:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Sustitución directa du = k dx", AppLanguage.Portuguese => "Substituição direta du = k dx", _ => "Direct substitution du = k dx" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Requiere despejar x en términos de u", AppLanguage.Portuguese => "Requer isolar x em termos de u", _ => "Solve for x in terms of u" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Sustitución trigonométrica", AppLanguage.Portuguese => "Substituição trigonométrica", _ => "Trigonometric substitution" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Potencias y polinomios internos", AppLanguage.Portuguese => "Potências e polinômios internos", _ => "Powers & inner polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Funciones trigonométricas", AppLanguage.Portuguese => "Funções trigonométricas", _ => "Trigonometric functions" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Exponenciales y logaritmos", AppLanguage.Portuguese => "Exponenciais e logaritmos", _ => "Exponentials & logarithms" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "def_riemann")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Método de aproximación / suma de Riemann:",
                    AppLanguage.Portuguese => "Método de aproximação / soma de Riemann:",
                    _ => "Approximation method / Riemann sum:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Extremo izquierdo (LRAM)", AppLanguage.Portuguese => "Extremo esquerdo (LRAM)", _ => "Left endpoint (LRAM)" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Extremo derecho (RRAM)", AppLanguage.Portuguese => "Extremo direito (RRAM)", _ => "Right endpoint (RRAM)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Punto medio (MRAM)", AppLanguage.Portuguese => "Ponto médio (MRAM)", _ => "Midpoint rule (MRAM)" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Regla del trapecio", AppLanguage.Portuguese => "Regra do trapézio", _ => "Trapezoidal rule" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Subintervalos iguales", AppLanguage.Portuguese => "Subintervalos iguais", _ => "Equal subintervals" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Desde tabla de valores", AppLanguage.Portuguese => "A partir de tabela", _ => "From table of values" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Desde función analítica", AppLanguage.Portuguese => "A partir de função analítica", _ => "From analytic function" };
                FuncCheck4.Visibility = Visibility.Collapsed;
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_int_area")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Región y curvas para calcular el área:",
                    AppLanguage.Portuguese => "Região e curvas para calcular a área:",
                    _ => "Region and curves for area calculation:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Área bajo la curva respecto al eje x", AppLanguage.Portuguese => "Área sob a curva em relação ao eixo x", _ => "Area under curve with respect to x" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Área entre dos curvas respecto a x", AppLanguage.Portuguese => "Área entre duas curvas em relação a x", _ => "Area between two curves w.r.t. x" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Área entre curvas respecto al eje y", AppLanguage.Portuguese => "Área entre curvas em relação ao eixo y", _ => "Area between curves w.r.t. y" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Curvas con límites dados", AppLanguage.Portuguese => "Curvas com limites dados", _ => "Curves with given bounds" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios", AppLanguage.Portuguese => "Polinômios", _ => "Polynomials" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Sombrear la región", AppLanguage.Portuguese => "Sombrear a região", _ => "Shade the region" };
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "app_int_volume")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Método de volumen de sólido de revolución:",
                    AppLanguage.Portuguese => "Método de volume de sólido de revolução:",
                    _ => "Volume of revolution method:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Método de discos (alrededor de un eje)", AppLanguage.Portuguese => "Método de discos (ao redor de um eixo)", _ => "Disk method (around axis)" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Método de arandelas (washers)", AppLanguage.Portuguese => "Método de arruelas (washers)", _ => "Washer method" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Método de cascarones cilíndricos (shells)", AppLanguage.Portuguese => "Método de cascas cilíndricas (shells)", _ => "Cylindrical shells method" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Secciones transversales conocidas", AppLanguage.Portuguese => "Seções transversais conhecidas", _ => "Known cross sections" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Rotación respecto al eje x", AppLanguage.Portuguese => "Rotação em torno do eixo x", _ => "Rotation about x-axis" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Rotación respecto al eje y", AppLanguage.Portuguese => "Rotação em torno do eixo y", _ => "Rotation about y-axis" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Rotación respecto a recta desplazada", AppLanguage.Portuguese => "Rotação em torno de reta deslocada", _ => "Rotation about shifted line" };
                FuncCheck4.Visibility = Visibility.Collapsed;
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "diff_eq_slope_fields")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Campos de pendientes (Slope Fields):",
                    AppLanguage.Portuguese => "Campos de inclinações (Slope Fields):",
                    _ => "Slope fields configuration:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Calcular pendientes en la cuadrícula", AppLanguage.Portuguese => "Calcular inclinações na grade", _ => "Calculate slopes on grid" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Emparejar ecuación con campo", AppLanguage.Portuguese => "Emparelhar equação com campo", _ => "Match equation to field" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Bosquejar curva solución por punto", AppLanguage.Portuguese => "Esboçar curva solução por ponto", _ => "Sketch solution curve through point" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "dy/dx = f(x)", AppLanguage.Portuguese => "dy/dx = f(x)", _ => "dy/dx = f(x)" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "dy/dx = f(y)", AppLanguage.Portuguese => "dy/dx = f(y)", _ => "dy/dx = f(y)" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "dy/dx = f(x, y)", AppLanguage.Portuguese => "dy/dx = f(x, y)", _ => "dy/dx = f(x, y)" };
                FuncCheck4.Visibility = Visibility.Collapsed;
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else if (arch == "diff_eq_separable")
            {
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Resolución de la ecuación diferencial:",
                    AppLanguage.Portuguese => "Resolução da equação diferencial:",
                    _ => "Differential equation solving:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Solución general (con constante C)", AppLanguage.Portuguese => "Solução geral (com constante C)", _ => "General solution (with C)" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Problema de valor inicial (PVI)", AppLanguage.Portuguese => "Problema de valor inicial (PVI)", _ => "Initial value problem (IVP)" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Crecimiento / decaimiento dy/dt = ky", AppLanguage.Portuguese => "Crescimento / decaimento dy/dt = ky", _ => "Exponential growth/decay" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };
                ComplexityRadio5.Visibility = Visibility.Collapsed;

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Separables de primer orden", AppLanguage.Portuguese => "Separáveis de primeira ordem", _ => "First order separable" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Polinomios en x e y", AppLanguage.Portuguese => "Polinômios em x e y", _ => "Polynomials in x and y" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Con funciones exponenciales", AppLanguage.Portuguese => "Com funções exponenciais", _ => "With exponentials" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Con funciones trigonométricas", AppLanguage.Portuguese => "Com funções trigonométricas", _ => "With trigonometric" };
                FuncCheck5.Visibility = Visibility.Collapsed;
                FuncCheck6.Visibility = Visibility.Collapsed;
                FuncCheck7.Visibility = Visibility.Collapsed;
            }
            else
            {
                // General fallback (Integration / diff_eq / etc.)
                ComplexityPrompt.Text = lang switch
                {
                    AppLanguage.Spanish => "Técnicas y configuración de problemas:",
                    AppLanguage.Portuguese => "Técnicas e configuração de problemas:",
                    _ => "Techniques and problem configuration:"
                };
                ComplexityRadio1.Content = lang switch { AppLanguage.Spanish => "Forma básica estándar", AppLanguage.Portuguese => "Forma básica padrão", _ => "Basic standard form" };
                ComplexityRadio2.Content = lang switch { AppLanguage.Spanish => "Sustitución / Regla de la cadena", AppLanguage.Portuguese => "Substituição / Regra da cadeia", _ => "Substitution / Chain rule" };
                ComplexityRadio3.Content = lang switch { AppLanguage.Spanish => "Métodos avanzados (partes / fracciones)", AppLanguage.Portuguese => "Métodos avançados", _ => "Advanced methods" };
                ComplexityRadio4.Content = lang switch { AppLanguage.Spanish => "Problemas aplicados con valores iniciales", AppLanguage.Portuguese => "Problemas aplicados com valores iniciais", _ => "Applied / Initial values" };
                ComplexityRadio5.Content = lang switch { AppLanguage.Spanish => "Cualquiera", AppLanguage.Portuguese => "Qualquer", _ => "Any" };

                FuncCheck1.Content = lang switch { AppLanguage.Spanish => "Polinomios / Potencias", AppLanguage.Portuguese => "Polinômios / Potências", _ => "Polynomial powers" };
                FuncCheck2.Content = lang switch { AppLanguage.Spanish => "Trigonométricas", AppLanguage.Portuguese => "Trigonométricas", _ => "Trigonometric" };
                FuncCheck3.Content = lang switch { AppLanguage.Spanish => "Exponenciales", AppLanguage.Portuguese => "Exponenciais", _ => "Exponential" };
                FuncCheck4.Content = lang switch { AppLanguage.Spanish => "Racionales", AppLanguage.Portuguese => "Racionais", _ => "Rational" };
                FuncCheck5.Content = lang switch { AppLanguage.Spanish => "Radicales", AppLanguage.Portuguese => "Radicais", _ => "Radicals" };
                FuncCheck6.Content = lang switch { AppLanguage.Spanish => "Logaritmos", AppLanguage.Portuguese => "Logaritmos", _ => "Logarithms" };
                FuncCheck7.Visibility = Visibility.Collapsed;
            }

            // Ensure chosen radio is not collapsed
            if (Parameters.ComplexityOptionIndex == 3 && ComplexityRadio4.Visibility != Visibility.Visible)
                Parameters.ComplexityOptionIndex = 0;
            if (Parameters.ComplexityOptionIndex == 4 && ComplexityRadio5.Visibility != Visibility.Visible)
                Parameters.ComplexityOptionIndex = 0;
        }

        private void UpdatePreviewDirectionsForTopic()
        {
            var lang = LocalizationService.CurrentLanguage;
            int idx = Parameters.ComplexityOptionIndex;
            string dir = _topic.DefaultDirections;

            if (_topic.TopicId == "deriv_avg_rates")
            {
                dir = idx switch
                {
                    1 => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle la ecuación de la recta secante que interseca los puntos dados.",
                        AppLanguage.Portuguese => "Para cada problema, encontre a equação da reta secante que intercepta os pontos dados.",
                        _ => "For each problem, find the equation of the secant line that intersects the given points."
                    },
                    2 => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle la tasa de cambio promedio y la recta secante en el intervalo dado.",
                        AppLanguage.Portuguese => "Para cada problema, encontre a taxa de variação média e a reta secante no intervalo dado.",
                        _ => "For each problem, find the average rate of change and the secant line over the given interval."
                    },
                    _ => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle la tasa de cambio promedio de la función en el intervalo dado.",
                        AppLanguage.Portuguese => "Para cada problema, encontre a taxa de variação média da função no intervalo dado.",
                        _ => "For each problem, find the average rate of change of the function over the given interval."
                    }
                };
            }
            else if (_topic.TopicId == "deriv_instant_rates" || _topic.TopicId == "app_tangent")
            {
                dir = idx switch
                {
                    1 => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle la pendiente de la función en el valor dado.",
                        AppLanguage.Portuguese => "Para cada problema, encontre a inclinação da função no valor dado.",
                        _ => "For each problem, find the slope of the function at the given value."
                    },
                    2 => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle la ecuación de la recta tangente a la función en el punto dado.",
                        AppLanguage.Portuguese => "Para cada problema, encontre a equação da reta tangente à função no ponto dado.",
                        _ => "For each problem, find the equation of the line tangent to the function at the given point."
                    },
                    3 => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle la ecuación de la recta normal a la función en el punto dado.",
                        AppLanguage.Portuguese => "Para cada problema, encontre a equação da reta normal à função no ponto dado.",
                        _ => "For each problem, find the equation of the line normal to the function at the given point."
                    },
                    4 => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle los puntos donde la recta tangente a la función es horizontal.",
                        AppLanguage.Portuguese => "Para cada problema, encontre os pontos onde a reta tangente à função é horizontal.",
                        _ => "For each problem, find the points where the tangent line to the function is horizontal."
                    },
                    _ => lang switch
                    {
                        AppLanguage.Spanish => "Para cada problema, halle la tasa de cambio instantánea de la función en el valor dado.",
                        AppLanguage.Portuguese => "Para cada problema, encontre a taxa de variação instantânea da função no valor dado.",
                        _ => "For each problem, find the instantaneous rate of change of the function at the given value."
                    }
                };
            }

            PreviewDirectionHeader.Text = dir;
            Parameters.CustomDirections = dir;
        }

        private void SyncUIFromParameters()
        {
            // Preset
            UpdatePresetButtonsHighlight(Parameters.Preset);

            // Question Type
            if (Parameters.QuestionType == QuestionType.MultipleChoice)
            {
                MultipleChoiceRadio.IsChecked = true;
            }
            else
            {
                FreeResponseRadio.IsChecked = true;
            }

            // Notation
            if (Parameters.Notation == "f(x)=") NotationFxRadio.IsChecked = true;
            else if (Parameters.Notation == "Either") NotationEitherRadio.IsChecked = true;
            else NotationYRadio.IsChecked = true;

            // Options
            VaryLettersCheck.IsChecked = Parameters.VaryLetters;
            BlankGraphCheck.IsChecked = Parameters.ProvideBlankGraph;
            FunctionGraphCheck.IsChecked = Parameters.ProvideFunctionGraph;

            // Complexity Radios
            switch (Parameters.ComplexityOptionIndex)
            {
                case 1: ComplexityRadio2.IsChecked = true; break;
                case 2: ComplexityRadio3.IsChecked = true; break;
                case 3: ComplexityRadio4.IsChecked = true; break;
                case 4: ComplexityRadio5.IsChecked = true; break;
                default: ComplexityRadio1.IsChecked = true; break;
            }
            UpdatePreviewDirectionsForTopic();

            // Function checkboxes by archetype
            string arch = GetTopicArchetype();
            if (arch == "deriv_avg_rates" || arch == "deriv_instant_rates")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeRational;
                FuncCheck3.IsChecked = Parameters.IncludeRadical;
                FuncCheck4.IsChecked = Parameters.IncludeTrig;
                FuncCheck5.IsChecked = Parameters.IncludeExp;
            }
            else if (arch == "deriv_definition")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeRational;
                FuncCheck3.IsChecked = Parameters.IncludeRadical;
            }
            else if (arch == "deriv_power")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeRational;
                FuncCheck3.IsChecked = Parameters.IncludeRadical;
                FuncCheck4.IsChecked = Parameters.IncludeRationalExpEasy;
            }
            else if (arch == "deriv_higher_order")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeTrig;
                FuncCheck3.IsChecked = Parameters.IncludeExp;
                FuncCheck4.IsChecked = Parameters.IncludeRational;
            }
            else if (arch == "deriv_product" || arch == "deriv_quotient")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeTrig;
                FuncCheck3.IsChecked = Parameters.IncludeExp;
                FuncCheck4.IsChecked = Parameters.IncludeLog;
            }
            else if (arch == "deriv_chain")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeTrig;
                FuncCheck3.IsChecked = Parameters.IncludeExp;
                FuncCheck4.IsChecked = Parameters.IncludeLog;
                FuncCheck5.IsChecked = Parameters.IncludeInverseTrig;
            }
            else if (arch == "deriv_implicit")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeQuadratic;
                FuncCheck3.IsChecked = Parameters.IncludeTrig;
                FuncCheck4.IsChecked = Parameters.IncludeRational;
            }
            else if (arch == "app_abs_extrema")
            {
                FuncCheck1.IsChecked = Parameters.IncludeQuadratic;
                FuncCheck2.IsChecked = Parameters.IncludeCubic;
                FuncCheck3.IsChecked = Parameters.IncludeQuartic;
                FuncCheck4.IsChecked = Parameters.IncludeRational;
                FuncCheck5.IsChecked = Parameters.IncludeRationalExpEasy;
                FuncCheck6.IsChecked = Parameters.IncludeRationalExpHard;
                FuncCheck7.IsChecked = Parameters.IncludeTrig;
            }
            else if (arch == "app_rel_extrema")
            {
                FuncCheck1.IsChecked = Parameters.IncludeQuadratic;
                FuncCheck2.IsChecked = Parameters.IncludeCubic;
                FuncCheck3.IsChecked = Parameters.IncludeQuartic;
                FuncCheck4.IsChecked = Parameters.IncludeRational;
                FuncCheck5.IsChecked = Parameters.IncludeRadical;
                FuncCheck6.IsChecked = Parameters.IncludeTrig;
            }
            else if (arch == "app_concavity")
            {
                FuncCheck1.IsChecked = Parameters.IncludeCubic;
                FuncCheck2.IsChecked = Parameters.IncludeQuartic;
                FuncCheck3.IsChecked = Parameters.IncludeRational;
                FuncCheck4.IsChecked = Parameters.IncludeRadical;
                FuncCheck5.IsChecked = Parameters.IncludeTrig;
            }
            else if (arch == "app_mvt")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeRational;
                FuncCheck3.IsChecked = Parameters.IncludeRadical;
                FuncCheck4.IsChecked = Parameters.IncludeTrig;
            }
            else if (arch == "app_optimization")
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeRational;
                FuncCheck3.IsChecked = Parameters.IncludeTrig;
                FuncCheck4.IsChecked = Parameters.IncludeExp;
            }
            else if (arch == "app_lhopital")
            {
                FuncCheck1.IsChecked = Parameters.IncludeRational;
                FuncCheck2.IsChecked = Parameters.IncludeTrig;
                FuncCheck3.IsChecked = Parameters.IncludeExp;
                FuncCheck4.IsChecked = Parameters.IncludeLog;
            }
            else if (arch.StartsWith("limits"))
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeRational;
                FuncCheck3.IsChecked = Parameters.IncludeRadical;
                FuncCheck4.IsChecked = Parameters.IncludeTrig;
                FuncCheck5.IsChecked = Parameters.IncludeAbs;
                FuncCheck6.IsChecked = Parameters.IncludeExp;
            }
            else
            {
                FuncCheck1.IsChecked = Parameters.IncludePolynomial;
                FuncCheck2.IsChecked = Parameters.IncludeTrig;
                FuncCheck3.IsChecked = Parameters.IncludeExp;
                FuncCheck4.IsChecked = Parameters.IncludeRational;
                FuncCheck5.IsChecked = Parameters.IncludeRadical;
                FuncCheck6.IsChecked = Parameters.IncludeLog;
            }
        }

        private void UpdatePresetButtonsHighlight(string preset)
        {
            void ApplyPresetStyle(Button btn, bool isSelected)
            {
                if (isSelected)
                {
                    btn.Background = (Brush)FindResource("ThemeAccent");
                    btn.Foreground = Brushes.White;
                    btn.BorderBrush = (Brush)FindResource("ThemeAccentHover");
                }
                else
                {
                    btn.Background = (Brush)FindResource("ThemeButtonBackground");
                    btn.Foreground = (Brush)FindResource("ThemeTextPrimary");
                    btn.BorderBrush = (Brush)FindResource("ThemeControlBorder");
                }
            }

            ApplyPresetStyle(EasyBtn, preset == "Easy");
            ApplyPresetStyle(MediumBtn, preset == "Medium");
            ApplyPresetStyle(HardBtn, preset == "Hard");
        }

        private void UpdateSamples()
        {
            if (!_isInitialized || SamplesContainer == null) return;

            SamplesContainer.Children.Clear();

            try
            {
                int count = 4;
                if (int.TryParse(NumQuestionsBox?.Text?.Trim() ?? "4", out int parsed) && parsed > 0 && parsed <= 50)
                {
                    count = parsed;
                }
                Parameters.QuestionCount = count;
                _currentPreviewQuestions = _topic.GenerateQuestions(count, Parameters.QuestionType, Parameters, _rng);
                var samples = _currentPreviewQuestions;

                for (int i = 0; i < samples.Count; i++)
                {
                    var q = samples[i];
                    q.Number = i + 1;

                    var qPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };

                    // 1. Question Number & Formula in LaTeX
                    var headerPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                    var numBlock = new TextBlock
                    {
                        Text = $"{q.Number})  ",
                        FontFamily = new FontFamily("Times New Roman, Cambria"),
                        FontSize = 14,
                        FontWeight = FontWeights.Normal,
                        Foreground = Brushes.Black,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    DockPanel.SetDock(numBlock, Dock.Left);
                    headerPanel.Children.Add(numBlock);

                    var formulaElem = WorksheetView.RenderMath(q.FormulaText, scale: 15.0, foreground: Brushes.Black);
                    headerPanel.Children.Add(formulaElem);
                    qPanel.Children.Add(headerPanel);

                    // 1.5 Optional function graph preview
                    if (Parameters.ProvideBlankGraph || Parameters.ProvideFunctionGraph)
                    {
                        var graph = new Controls.FunctionGraphControl
                        {
                            IsBlank = Parameters.ProvideBlankGraph && !Parameters.ProvideFunctionGraph,
                            FunctionExpr = !string.IsNullOrWhiteSpace(q.GraphFormula) ? q.GraphFormula : Generators.MathExpressionEvaluator.ExtractFormula(q.FormulaText),
                            XMin = -6.0,
                            XMax = 6.0,
                            YMin = -6.0,
                            YMax = 6.0,
                            Holes = q.GraphHoles,
                            Points = q.GraphPoints,
                            Width = 180,
                            Height = 180,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(22, 4, 0, 8)
                        };
                        qPanel.Children.Add(graph);
                    }

                    // 2. Multiple choice options (if Multiple Choice)
                    if (Parameters.QuestionType == QuestionType.MultipleChoice && q.Choices.Count > 0)
                    {
                        bool isLong = q.Choices.Any(c => c.Text.Length > 14 || c.Text.Contains(@"\text") || c.Text.Contains(' '));
                        var choicesGrid = new Grid { Margin = new Thickness(22, 4, 0, 6) };

                        if (isLong)
                        {
                            choicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            for (int j = 0; j < q.Choices.Count; j++)
                            {
                                var c = q.Choices[j];
                                var choicePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                                var letterBlock = new TextBlock
                                {
                                    Text = $"{c.Letter})  ",
                                    FontFamily = new FontFamily("Times New Roman, Cambria"),
                                    FontSize = 12,
                                    FontWeight = c.IsCorrect ? FontWeights.Bold : FontWeights.Normal,
                                    Foreground = c.IsCorrect ? new SolidColorBrush(Color.FromRgb(200, 20, 20)) : Brushes.Black,
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                choicePanel.Children.Add(letterBlock);

                                var choiceMath = WorksheetView.RenderMath(c.Text, scale: 12.5,
                                    foreground: c.IsCorrect ? new SolidColorBrush(Color.FromRgb(200, 20, 20)) : Brushes.Black,
                                    isBold: c.IsCorrect);
                                choicePanel.Children.Add(choiceMath);

                                choicesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                Grid.SetRow(choicePanel, j);
                                Grid.SetColumn(choicePanel, 0);
                                choicesGrid.Children.Add(choicePanel);
                            }
                        }
                        else
                        {
                            choicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            choicesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                            for (int j = 0; j < q.Choices.Count; j++)
                            {
                                var c = q.Choices[j];
                                var choicePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                                var letterBlock = new TextBlock
                                {
                                    Text = $"{c.Letter})  ",
                                    FontFamily = new FontFamily("Times New Roman, Cambria"),
                                    FontSize = 12,
                                    FontWeight = c.IsCorrect ? FontWeights.Bold : FontWeights.Normal,
                                    Foreground = c.IsCorrect ? new SolidColorBrush(Color.FromRgb(200, 20, 20)) : Brushes.Black,
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                choicePanel.Children.Add(letterBlock);

                                var choiceMath = WorksheetView.RenderMath(c.Text, scale: 12.5,
                                    foreground: c.IsCorrect ? new SolidColorBrush(Color.FromRgb(200, 20, 20)) : Brushes.Black,
                                    isBold: c.IsCorrect);
                                choicePanel.Children.Add(choiceMath);

                                int row = j / 2;
                                int col = j % 2;
                                if (row >= choicesGrid.RowDefinitions.Count)
                                {
                                    choicesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                }
                                Grid.SetRow(choicePanel, row);
                                Grid.SetColumn(choicePanel, col);
                                choicesGrid.Children.Add(choicePanel);
                            }
                        }

                        qPanel.Children.Add(choicesGrid);
                    }

                    // 3. Red teacher answers below (Free Response or MC answer key)
                    var redPanel = new StackPanel { Margin = new Thickness(22, 2, 0, 0) };
                    var ansLines = q.AnswerText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var ansLine in ansLines)
                    {
                        var redAnsElem = WorksheetView.RenderMath(ansLine.Trim(), scale: 13.5,
                            foreground: new SolidColorBrush(Color.FromRgb(220, 25, 25)), isBold: false);
                        redPanel.Children.Add(redAnsElem);
                    }

                    if (!string.IsNullOrEmpty(q.SolutionSteps))
                    {
                        var stepsElem = WorksheetView.RenderMixedTextAndMath(q.SolutionSteps, scale: 11.5,
                            foreground: new SolidColorBrush(Color.FromRgb(150, 30, 30)));
                        redPanel.Children.Add(stepsElem);
                    }

                    qPanel.Children.Add(redPanel);

                    SamplesContainer.Children.Add(qPanel);
                }
            }
            catch (Exception ex)
            {
                SamplesContainer.Children.Add(new TextBlock
                {
                    Text = $"Error generating preview: {ex.Message}",
                    Foreground = Brushes.Red
                });
            }
        }

        #region Event Handlers
        private void OnPresetEasyClick(object sender, RoutedEventArgs e)
        {
            Parameters.Preset = "Easy";
            Parameters.ComplexityOptionIndex = 0;
            UpdatePresetButtonsHighlight("Easy");

            ComplexityRadio1.IsChecked = true;
            FuncCheck1.IsChecked = true;
            FuncCheck2.IsChecked = false;
            FuncCheck3.IsChecked = false;
            FuncCheck4.IsChecked = false;
            FuncCheck5.IsChecked = false;
            FuncCheck6.IsChecked = false;
            FuncCheck7.IsChecked = false;

            OnFuncCheckChanged(null, null);
        }

        private void OnPresetMediumClick(object sender, RoutedEventArgs e)
        {
            Parameters.Preset = "Medium";
            Parameters.ComplexityOptionIndex = 0;
            UpdatePresetButtonsHighlight("Medium");

            ComplexityRadio1.IsChecked = true;
            FuncCheck1.IsChecked = true;
            FuncCheck2.IsChecked = true;
            FuncCheck3.IsChecked = false;
            FuncCheck4.IsChecked = false;
            FuncCheck5.IsChecked = false;
            FuncCheck6.IsChecked = false;
            FuncCheck7.IsChecked = false;

            OnFuncCheckChanged(null, null);
        }

        private void OnPresetHardClick(object sender, RoutedEventArgs e)
        {
            Parameters.Preset = "Hard";
            Parameters.ComplexityOptionIndex = 2;
            UpdatePresetButtonsHighlight("Hard");

            ComplexityRadio3.IsChecked = true;
            FuncCheck1.IsChecked = true;
            FuncCheck2.IsChecked = true;
            FuncCheck3.IsChecked = true;
            FuncCheck4.IsChecked = true;
            FuncCheck5.IsChecked = false;
            FuncCheck6.IsChecked = false;
            FuncCheck7.IsChecked = false;

            OnFuncCheckChanged(null, null);
        }

        private void OnQuestionTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            Parameters.QuestionType = (MultipleChoiceRadio.IsChecked == true) ? QuestionType.MultipleChoice : QuestionType.FreeResponse;
            UpdateSamples();
        }

        private void OnNotationChanged(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            if (NotationFxRadio.IsChecked == true) Parameters.Notation = "f(x)=";
            else if (NotationEitherRadio.IsChecked == true) Parameters.Notation = "Either";
            else Parameters.Notation = "y=";

            UpdateSamples();
        }

        private void OnOptionToggled(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            if (sender == BlankGraphCheck && BlankGraphCheck.IsChecked == true)
            {
                FunctionGraphCheck.IsChecked = false;
            }
            else if (sender == FunctionGraphCheck && FunctionGraphCheck.IsChecked == true)
            {
                BlankGraphCheck.IsChecked = false;
            }

            Parameters.VaryLetters = VaryLettersCheck.IsChecked == true;
            Parameters.ProvideBlankGraph = BlankGraphCheck.IsChecked == true;
            Parameters.ProvideFunctionGraph = FunctionGraphCheck.IsChecked == true;
            UpdateSamples();
        }

        private void OnComplexityRadioChanged(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            if (ComplexityRadio1.IsChecked == true) Parameters.ComplexityOptionIndex = 0;
            else if (ComplexityRadio2.IsChecked == true) Parameters.ComplexityOptionIndex = 1;
            else if (ComplexityRadio3.IsChecked == true) Parameters.ComplexityOptionIndex = 2;
            else if (ComplexityRadio4.IsChecked == true) Parameters.ComplexityOptionIndex = 3;
            else if (ComplexityRadio5.IsChecked == true) Parameters.ComplexityOptionIndex = 4;

            UpdatePreviewDirectionsForTopic();
            UpdateSamples();
        }

        private void OnFuncCheckChanged(object? sender, RoutedEventArgs? e)
        {
            if (!_isInitialized) return;
            string arch = GetTopicArchetype();

            if (arch == "deriv_avg_rates" || arch == "deriv_instant_rates")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeRational = FuncCheck2.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck3.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck4.IsChecked == true;
                Parameters.IncludeExp = FuncCheck5.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else if (arch == "deriv_definition")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeRational = FuncCheck2.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck3.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else if (arch == "deriv_power")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeRational = FuncCheck2.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck3.IsChecked == true;
                Parameters.IncludeRationalExpEasy = FuncCheck4.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else if (arch == "deriv_higher_order")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck2.IsChecked == true;
                Parameters.IncludeExp = FuncCheck3.IsChecked == true;
                Parameters.IncludeRational = FuncCheck4.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else if (arch == "deriv_product" || arch == "deriv_quotient")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck2.IsChecked == true;
                Parameters.IncludeExp = FuncCheck3.IsChecked == true;
                Parameters.IncludeLog = FuncCheck4.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else if (arch == "deriv_chain")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck2.IsChecked == true;
                Parameters.IncludeExp = FuncCheck3.IsChecked == true;
                Parameters.IncludeLog = FuncCheck4.IsChecked == true;
                Parameters.IncludeInverseTrig = FuncCheck5.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else if (arch == "deriv_implicit")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeQuadratic = FuncCheck2.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck3.IsChecked == true;
                Parameters.IncludeRational = FuncCheck4.IsChecked == true;
            }
            else if (arch == "app_abs_extrema")
            {
                Parameters.IncludeQuadratic = FuncCheck1.IsChecked == true;
                Parameters.IncludeCubic = FuncCheck2.IsChecked == true;
                Parameters.IncludeQuartic = FuncCheck3.IsChecked == true;
                Parameters.IncludeRational = FuncCheck4.IsChecked == true;
                Parameters.IncludeRationalExpEasy = FuncCheck5.IsChecked == true;
                Parameters.IncludeRationalExpHard = FuncCheck6.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck7.IsChecked == true;
                Parameters.IncludePolynomial = Parameters.IncludeQuadratic || Parameters.IncludeCubic || Parameters.IncludeQuartic;
            }
            else if (arch == "app_rel_extrema")
            {
                Parameters.IncludeQuadratic = FuncCheck1.IsChecked == true;
                Parameters.IncludeCubic = FuncCheck2.IsChecked == true;
                Parameters.IncludeQuartic = FuncCheck3.IsChecked == true;
                Parameters.IncludeRational = FuncCheck4.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck5.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck6.IsChecked == true;
                Parameters.IncludePolynomial = Parameters.IncludeQuadratic || Parameters.IncludeCubic || Parameters.IncludeQuartic;
            }
            else if (arch == "app_concavity")
            {
                Parameters.IncludeCubic = FuncCheck1.IsChecked == true;
                Parameters.IncludeQuartic = FuncCheck2.IsChecked == true;
                Parameters.IncludeRational = FuncCheck3.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck4.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck5.IsChecked == true;
                Parameters.IncludePolynomial = Parameters.IncludeCubic || Parameters.IncludeQuartic;
            }
            else if (arch == "app_mvt")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeRational = FuncCheck2.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck3.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck4.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else if (arch == "app_optimization")
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeRational = FuncCheck2.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck3.IsChecked == true;
                Parameters.IncludeExp = FuncCheck4.IsChecked == true;
            }
            else if (arch == "app_lhopital")
            {
                Parameters.IncludeRational = FuncCheck1.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck2.IsChecked == true;
                Parameters.IncludeExp = FuncCheck3.IsChecked == true;
                Parameters.IncludeLog = FuncCheck4.IsChecked == true;
            }
            else if (arch.StartsWith("limits"))
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeRational = FuncCheck2.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck3.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck4.IsChecked == true;
                Parameters.IncludeAbs = FuncCheck5.IsChecked == true;
                Parameters.IncludeExp = FuncCheck6.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }
            else
            {
                Parameters.IncludePolynomial = FuncCheck1.IsChecked == true;
                Parameters.IncludeTrig = FuncCheck2.IsChecked == true;
                Parameters.IncludeExp = FuncCheck3.IsChecked == true;
                Parameters.IncludeRational = FuncCheck4.IsChecked == true;
                Parameters.IncludeRadical = FuncCheck5.IsChecked == true;
                Parameters.IncludeLog = FuncCheck6.IsChecked == true;
                Parameters.IncludeQuadratic = Parameters.IncludePolynomial;
            }

            UpdateSamples();
        }

        private void OnNumQuestionsChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            if (int.TryParse(NumQuestionsBox.Text.Trim(), out int count) && count > 0 && count <= 50)
            {
                Parameters.QuestionCount = count;
                UpdateSamples();
            }
        }

        private void OnMoreSamplesClick(object sender, RoutedEventArgs e)
        {
            UpdateSamples();
            SamplesScrollViewer?.ScrollToTop();
        }

        private void OnAddContinueClick(object sender, RoutedEventArgs e)
        {
            int count = 4;
            if (int.TryParse(NumQuestionsBox.Text.Trim(), out int parsed) && parsed > 0 && parsed <= 50)
            {
                count = parsed;
            }
            Parameters.QuestionCount = count;
            Parameters.CustomDirections = PreviewDirectionHeader.Text;

            List<Question> accepted;
            if (_currentPreviewQuestions != null && _currentPreviewQuestions.Count > 0)
            {
                if (_currentPreviewQuestions.Count == count)
                {
                    accepted = _currentPreviewQuestions.Select(q => q.Clone()).ToList();
                }
                else if (_currentPreviewQuestions.Count > count)
                {
                    accepted = _currentPreviewQuestions.Take(count).Select(q => q.Clone()).ToList();
                }
                else
                {
                    accepted = _currentPreviewQuestions.Select(q => q.Clone()).ToList();
                    var extra = _topic.GenerateQuestions(count - _currentPreviewQuestions.Count, Parameters.QuestionType, Parameters, _rng);
                    accepted.AddRange(extra.Select(q => q.Clone()));
                }
            }
            else
            {
                accepted = _topic.GenerateQuestions(count, Parameters.QuestionType, Parameters, _rng);
            }

            for (int i = 0; i < accepted.Count; i++)
            {
                accepted[i].Number = i + 1;
            }

            QuestionsAdded?.Invoke(count, Parameters.QuestionType, Parameters.Clone(), accepted);
            UpdateSamples();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            int count = 4;
            if (int.TryParse(NumQuestionsBox.Text.Trim(), out int parsed) && parsed > 0 && parsed <= 50)
            {
                count = parsed;
            }
            Parameters.QuestionCount = count;
            Parameters.CustomDirections = PreviewDirectionHeader.Text;

            List<Question> accepted;
            if (_currentPreviewQuestions != null && _currentPreviewQuestions.Count > 0)
            {
                if (_currentPreviewQuestions.Count == count)
                {
                    accepted = _currentPreviewQuestions.Select(q => q.Clone()).ToList();
                }
                else if (_currentPreviewQuestions.Count > count)
                {
                    accepted = _currentPreviewQuestions.Take(count).Select(q => q.Clone()).ToList();
                }
                else
                {
                    accepted = _currentPreviewQuestions.Select(q => q.Clone()).ToList();
                    var extra = _topic.GenerateQuestions(count - _currentPreviewQuestions.Count, Parameters.QuestionType, Parameters, _rng);
                    accepted.AddRange(extra.Select(q => q.Clone()));
                }
            }
            else
            {
                accepted = _topic.GenerateQuestions(count, Parameters.QuestionType, Parameters, _rng);
            }

            for (int i = 0; i < accepted.Count; i++)
            {
                accepted[i].Number = i + 1;
            }

            QuestionsAdded?.Invoke(count, Parameters.QuestionType, Parameters.Clone(), accepted);
            try { DialogResult = true; } catch (InvalidOperationException) { }
            try { Close(); } catch (InvalidOperationException) { }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            try { DialogResult = false; } catch (InvalidOperationException) { }
            try { Close(); } catch (InvalidOperationException) { }
        }
        #endregion
    }
}
