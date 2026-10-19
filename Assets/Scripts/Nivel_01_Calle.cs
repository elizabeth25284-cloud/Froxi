// Nivel 1. Un archivo por nivel: para agregar uno nuevo copia este archivo,
// cambia el numero y el nombre de la clase (Nivel_NN_Nombre) y edita el mapa.
// Letras:  . vacio   # suelo   = plataforma (se atraviesa desde abajo)   B bloque solido
//          P Froxi   f fruta o verdura   j chatarra que camina   x chatarra quieta   G meta
public static class Nivel_01_Calle
{
    public static NivelDatos Datos()
    {
        return new NivelDatos
        {
            nombre = "La calle", descripcion = "Recoge alimentos saludables y llega a casa. Cuidado con la comida chatarra.",
            fondo = "fondo_calle",
            top = "l1_acera_top",
            cuerpo = "l1_acera_cuerpo",
            plataformaL = "l1_toldo_izq",
            plataformaM = "l1_toldo_med",
            plataformaR = "l1_toldo_der",
            bloque = "l1_caja",
            meta = "meta_casa",
            mapa = new string[]
            {
                "....................................................................................................",
                "....................................................................................................",
                "....................................................................................................",
                "....................................................................................................",
                "....................................................................................................",
                ".............fff.......................ff.........f..........f..........fff...f.....................",
                ".............===..................f...====...................B..........===..===....................",
                "...P....fff........ff....j...ff...B.........j..ff.x...ff....BBB.....j..............j..fff......G....",
                "####################################################################################################",
                "####################################################################################################",
            }
        };
    }
}
