Shader "ProceduralGeneration/VertexColor"
{
    // Properties // daca vreau sa introduc date din editor pt shader
    // {
    //     [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    //     [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    // }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200                         // Level of detail  = cat de complex consideram shader-ul

        Pass
        {
            // HLSLPROGRAM      //  HLSL    = High-Level Shading Language (Microsoft) (intalnit la URP)
            CGPROGRAM           //  CG      = C for Graphics (NVIDIA)

            // Parametrii
            #pragma vertex vert         // vertex shader    = ia fiecare punct 3D si il transpune pe ecranul 2D                                 = unde stau lucrurile
            #pragma fragment frag       // fragment shader  = ia fiecare pixel de pe ecran acoperit de obiect si decide ce culoare sa ii dea    = cum arata lucrurile

            #include "UnityCG.cginc"    // contine functiile UnityObjectToClipPos() si UnityObjectToWorldNormal()

            struct appData              // = ce info primeste vert
            {
                // numele variabilei poate fi orice, dar constanta NU (spune placii video ce se afla in variabila)
                float4 vertex: POSITION;
                float4 color: COLOR;
                float3 normal: NORMAL;
            };

            struct v2f                  // vertex to fragment = ce info returneaza vert si primeste frag
            {
                float4 vertex: SV_POSITION;     // SV = System Value = pozitia finala pe ecran
                float4 color: COLOR;
                float3 worldNormal: TEXCOORD;   // = Texture Coodinate
                                                // sau TEXCOORD0, TEXCOORD1, TEXCOORD2, ... = canal / registru al placii video
            };

            v2f vert (appData v)        // trece in spatiul clip space (ecranul 2D)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);          // coordonatele 2D de pe ecranul jucatorului corespunzatoare obiectului 3D
                o.color = v.color;
                o.worldNormal = UnityObjectToWorldNormal(v.normal); // directia vectorului normala, raportata la intreaga lume (pt umbre)
                // normal       = normala in vertexul v al obiectului                           ex: (0, 1, 0) pt normala in punctul (2, 10, 3) varful unui munte                    | (0.71, 0.71, 0) pt normala intr-un punct de pe panta unui munte de 45 de grade (=> cos(45) = sin(45) = 0.71)
                // worldNormal  = aceasi normala, dar rotita odaca cu obiectul daca e cazul     ex: (1, 0, 0) pt normala raportata la lume, daca muntele e rotit 90 de grade pe ox  | (0.71, 0.71, 0) pt normala raportata la lume, daca muntele nu e rotit
                return o;
            }

            fixed4 frag (v2f i): SV_Target      // folosesc culoarea intr-o textura, nu direct pe ecran
            {
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz); 
                // _WorldSpaceLightPos0 = directia luminii principale
                
                float lambert = saturate(dot(i.worldNormal, lightDir)) * 0.5 + 0.5;
                // dot(a, b) = cos(unghiul dintre cei 2 vectori)
                //         a = worldNormal  = directia normalei in vertex raportata la lume
                //         b = lightDir     = directia din care vine lumina lumii
                // saturate() = limiteaza valoarea obtinuta in [0.0, 1.0] (pt ca cos(u) poate fi negativ)
                // Half-Lambert = * 0.5 + 0.5
                // => din 0-1 in 0.5-1
                // => fara zone prea intunecate
                return i.color * lambert;
            }

            // ENDHLSL
            ENDCG
        }
    }
}
