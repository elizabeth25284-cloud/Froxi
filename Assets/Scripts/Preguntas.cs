// Banco de preguntas. Para agregar una nueva copia una linea y cambia los textos.
// La respuesta correcta SIEMPRE se escribe en 'correcta' (el juego la mezcla solo).
public class Pregunta
{
    public string texto, correcta, mala1, mala2, explicacion;
}

public static class BancoPreguntas
{
    static Pregunta Q(string t, string ok, string m1, string m2, string exp)
    {
        return new Pregunta { texto = t, correcta = ok, mala1 = m1, mala2 = m2, explicacion = exp };
    }

    public static readonly Pregunta[] Todas = new Pregunta[]
    {
        Q("¿Cuál es la mejor bebida para hidratarte todos los días?", "Agua simple", "Refresco", "Jugo industrializado",
          "El agua simple hidrata sin azúcar añadida."),
        Q("¿Cuántas porciones de frutas y verduras se recomienda comer al día?", "Al menos 5", "Solo 1", "Ninguna",
          "Se recomienda comer mínimo 5 porciones de frutas y verduras al día."),
        Q("¿Qué nutriente es la principal fuente de energía del cuerpo?", "Los carbohidratos", "La sal", "El agua",
          "Los carbohidratos (cereales, frutas, tubérculos) dan la energía principal."),
        Q("¿Qué nutriente ayuda a construir y reparar los músculos?", "Las proteínas", "Los azúcares", "Las grasas saturadas",
          "Las proteínas (huevo, pescado, frijol, pollo) forman y reparan tejidos."),
        Q("El exceso de comida chatarra puede provocar:", "Obesidad y diabetes", "Huesos más fuertes", "Más estatura",
          "Mucha azúcar, sal y grasa aumentan el riesgo de obesidad, diabetes y problemas del corazón."),
        Q("¿Cuál de estos es un alimento ultraprocesado?", "Papas fritas de bolsa", "Zanahoria", "Lentejas",
          "Las botanas de bolsa tienen mucha sal, grasa y aditivos."),
        Q("¿Qué vitamina abunda en la naranja y el limón?", "Vitamina C", "Vitamina K", "Vitamina B12",
          "La vitamina C ayuda a las defensas y a cicatrizar."),
        Q("¿Para qué sirve el calcio?", "Para huesos y dientes fuertes", "Para ver mejor", "Para correr más rápido",
          "La leche, el yogurt y las tortillas de maíz nixtamalizado aportan calcio."),
        Q("¿Cuál es un desayuno más saludable?", "Fruta, avena y leche", "Refresco y galletas", "No desayunar",
          "Un desayuno completo te da energía para concentrarte en clases."),
        Q("Si te saltas comidas muy seguido, lo más probable es que:", "Tengas cansancio y más antojos", "Tengas más energía", "Bajes de peso sin problemas",
          "Saltarse comidas provoca cansancio y que luego comas de más."),
        Q("¿Cuál tiene más azúcar añadida?", "Un refresco de lata", "Un vaso de agua", "Una rebanada de jícama",
          "Un refresco puede tener el equivalente a varias cucharadas de azúcar."),
        Q("El Plato del Bien Comer agrupa los alimentos en:", "3 grupos", "10 grupos", "No existe esa guía",
          "Son 3 grupos: verduras y frutas, cereales, y leguminosas con alimentos de origen animal."),
        Q("¿Cuál es una grasa saludable?", "El aguacate", "La manteca de cerdo", "La grasa de las papas fritas",
          "El aguacate, las nueces y el aceite de oliva tienen grasas buenas."),
        Q("¿Para qué sirve la fibra?", "Ayuda a la digestión", "Aumenta el azúcar en sangre", "Quita la sed",
          "La fibra (frutas, verduras, frijoles, avena) ayuda a tu digestión."),
        Q("¿Cuánta agua se recomienda tomar al día aproximadamente?", "De 1.5 a 2 litros", "Solo un vaso", "Más de 5 litros",
          "Entre 1.5 y 2 litros, según tu actividad y el clima."),
        Q("¿Cuál de estos alimentos tiene más proteína?", "El huevo", "El refresco", "La dona",
          "El huevo es una proteína completa y económica."),
        Q("Un buen refrigerio entre comidas es:", "Zanahoria con limón", "Papas con chile y refresco", "Una dona glaseada",
          "Las verduras y frutas te llenan sin tanta azúcar ni grasa."),
        Q("¿Qué hace el hierro en nuestro cuerpo?", "Ayuda a llevar oxígeno en la sangre", "Da sabor a la comida", "Hace crecer el cabello",
          "El hierro está en frijoles, lentejas, hígado y espinacas."),
        Q("El exceso de azúcar durante años puede contribuir a:", "Diabetes tipo 2", "Gripa", "Alergia al polen",
          "El consumo alto de azúcar aumenta el riesgo de diabetes tipo 2."),
        Q("Para cuidar el corazón conviene reducir:", "La sal y las grasas saturadas", "Las frutas", "El agua",
          "Menos sal y grasas saturadas ayudan a cuidar tu corazón."),
        Q("¿Cuál es una buena fuente de proteína vegetal?", "Frijoles y lentejas", "Refresco", "Caramelos",
          "Las leguminosas dan proteína y fibra."),
        Q("Leer las etiquetas de los alimentos sirve para:", "Conocer azúcar, sodio y calorías", "Saber de qué color es", "Nada, no sirve",
          "La etiqueta te dice cuánto azúcar, sal y grasa tiene lo que comes."),
        Q("Las porciones grandes de comida rápida suelen tener:", "Muchas calorías y poca fibra", "Mucha fibra y pocas calorías", "Solo verduras",
          "La comida rápida suele ser alta en calorías, grasa y sal."),
        Q("En la cooperativa de la escuela, ¿qué conviene más para tomar?", "Agua natural", "Refresco grande", "Bebida energética",
          "El agua natural es la mejor opción para tu salud."),
        Q("¿Cuál de estos alimentos tiene mucha vitamina A?", "La zanahoria", "El refresco", "Las papas fritas",
          "La zanahoria tiene betacarotenos que el cuerpo convierte en vitamina A."),
        Q("¿Qué es mejor para cocinar alimentos de forma saludable?", "Al horno, cocidos o a la plancha", "Fritos en mucho aceite", "Empanizados y fritos",
          "Cocinar sin mucho aceite reduce las grasas."),
    };
}
