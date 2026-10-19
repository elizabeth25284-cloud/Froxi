// Nivel 2. Un archivo por nivel: para agregar uno nuevo copia este archivo,
// cambia el numero y el nombre de la clase (Nivel_NN_Nombre) y edita el mapa.
// Letras:  . vacio   # suelo   = plataforma (se atraviesa desde abajo)   B bloque solido
//          P Froxi   f fruta o verdura   j chatarra que camina   x chatarra quieta   G meta
public static class Nivel_02_Escuela
{
    public static NivelDatos Datos()
    {
        return new NivelDatos
        {
            nombre = "La escuela", descripcion = "Ya casi llegas. Cruza los huecos y cuida tu alimentacion en el patio.",
            fondo = "fondo_escuela",
            top = "l2_pasto_top",
            cuerpo = "l2_ladrillo",
            plataformaL = "l2_banca_izq",
            plataformaM = "l2_banca_med",
            plataformaR = "l2_banca_der",
            bloque = "l2_casillero",
            meta = "meta_bandera",
            mapa = new string[]
            {
                "............................................................................................................................",
                "............................................................................................................................",
                "............................................................................................................................",
                "............................................................................................................................",
                ".............................................................................fff............................................",
                "...........fff........................ff...f.........ff....f............fff..===..........f.........fff.....................",
                "...........===......f.......fff......====............==....B............===.........................===............f........",
                "...P....ff......j...B.............j........x...j.ff.......BBB...j..ff.j..............j....x..ff.j...........j..fff.B...G....",
                "############################...#####################....######################...###################...#####################",
                "############################...#####################....######################...###################...#####################",
            }
        };
    }
}
