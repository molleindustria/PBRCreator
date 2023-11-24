/*
 * Mash-up of EasyTextureAssign by Cody Anderson
 * and Metallic-Smoothness combinator by Todd Gillissie 
 */

namespace PBRCreator
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using System.Text.RegularExpressions;

    public class PBRCreator
    {
        #region VARIABLES
        private const string defaultMaterial = "Default-Material";

        private static List<Material> selectedMats = new List<Material>();
        private static Material mat = null;

        private static bool foundTextures = false;
        private static Texture albedo, metallic, emission, normal, height, occlusion = null;
        #endregion



        /// <summary>
        /// Creates a new material and assigns selected textures
        /// </summary>
        [MenuItem("Assets/Create Standard Material from Selection ", false, 101)]
        private static void CreateStdMaterial()
        {
            CreateMatFromTexture();
        }

        [MenuItem("Assets/Create TriPlanar Material from Selection ", false, 101)]
        private static void CreateTriMaterial()
        {
            CreateMatFromTexture("StandardTriplanar");
        }


        private static void CreateMatFromTexture(string shader = "Standard")
        {
            //  get selection
            Object[] objects = Selection.objects;

            //  find material and textures
            FindTextures(objects);
            if (!foundTextures) return;

            //  prompt
            //if (EditorUtility.DisplayDialog("Create NEW Material?", "Create new material from selection?\n\nYou cannot undo this action.", "Create", "Cancel"))

            Material material = new Material(Shader.Find(shader));

            string path = AssetDatabase.GetAssetPath(albedo);
            path = System.IO.Path.GetDirectoryName(path);

            string fileName = "";
            if (albedo != null) fileName = albedo.name;
            else if (albedo != null) fileName = albedo.name;
            else if (metallic != null) fileName = metallic.name;
            else if (normal != null) fileName = normal.name;
            else if (height != null) fileName = height.name;
            else if (occlusion != null) fileName = occlusion.name;
            else if (emission != null) fileName = emission.name;

            string fileNameTest = fileName.ToLower();

            fileNameTest = fileNameTest.Replace(albedo.name.ToLower(), "^#$^");

            int index = fileNameTest.IndexOf("^#$^");
            if (index > 0)
                fileName = fileName.Substring(0, index);

            int index2 = fileName.IndexOfAny(new char[] { '-', '_', ' ' });
            if (index2 > 0)
                fileName = fileName.Substring(0, index2);

            AssetDatabase.CreateAsset(material, path + "/" + fileName + "_" + shader + "Material.mat");
            mat = material;
            AssignTextures(false);

        }

        /// <summary>
        /// Find textures and material in selection
        /// </summary>
        private static void FindTextures(Object[] objects)
        {
            if (objects.Length == 0) return;

            HashSet<Object> allObjects = new HashSet<Object>(objects);

            //  find textures in folder
            string path = "";
            foreach (var obj in objects)
            {
                path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
                if (path.Length > 0)
                {
                    if (System.IO.Directory.Exists(path))
                    {
                        string[] guids2 = AssetDatabase.FindAssets("t:texture2D", new[] { path });
                        foreach (var guid in guids2)
                        {
                            allObjects.Add(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object)));
                        }
                    }
                }
            }

            //  reset variables
            mat = null;
            foundTextures = false;
            albedo = metallic = emission = normal = height = occlusion = null;

            //  find textures and material
            foreach (Object obj in allObjects)
            {
                //  find material
                if (obj is Material && mat == null)
                {
                    mat = obj as Material;
                }
                //  find material on MeshRenderer
                else if (obj is GameObject && mat == null)
                {
                    GameObject go = (GameObject)obj;
                    Renderer renderer = go.GetComponent<Renderer>();
                    if (renderer != null && (renderer is SkinnedMeshRenderer || renderer is MeshRenderer))
                    {
                        mat = renderer.sharedMaterial;
                    }
                }
                //  find textures
                else if (obj is Texture)
                {
                    Texture texture = obj as Texture;

                    if (CheckName(texture.name, ETSettings.ALBEDO_NAMES) && albedo == null)
                    {
                        albedo = texture;
                        foundTextures = true;
                    }
                    else if (CheckName(texture.name, ETSettings.EMISSION_NAMES) && emission == null)
                    {
                        emission = texture;
                        foundTextures = true;
                    }
                    else if (CheckName(texture.name, ETSettings.METALLIC_NAMES) && metallic == null)
                    {
                        metallic = texture;
                        foundTextures = true;
                    }
                    else if (CheckName(texture.name, ETSettings.NORMAL_NAMES) && normal == null)
                    {
                        normal = texture;
                        foundTextures = true;
                    }
                    else if (CheckName(texture.name, ETSettings.HEIGHT_NAMES) && height == null)
                    {
                        //height sucks
                        //height = texture;
                        //foundTextures = true;
                    }
                    else if (CheckName(texture.name, ETSettings.OCCLUSION_NAMES) && occlusion == null)
                    {
                        occlusion = texture;
                        foundTextures = true;
                    }
                    else if (CheckName(texture.name, ETSettings.SMOOTHNESS_NAMES) && metallic == null)
                    {
                        metallic = texture;
                        foundTextures = true;
                    }
                    else if (CheckName(texture.name, ETSettings.METALLIC_SMOOTHNESS_NAMES))
                    {
                        //overrides previous metallic and/or smoothness
                        metallic = texture;
                        foundTextures = true;
                    }


                }
            }
        }

        private static bool CheckName(string fileName, string[] keywords)
        {
            bool found = false;

            foreach (string s in keywords)
            {
                if (fileName.ToLower().Contains(s.ToLower()))
                    found = true;
            }
            return found;
        }

        /// <summary>
        /// Displays confirm window and assigns textures
        /// </summary>
        private static void AssignTextures(bool undo = true)
        {
            //  stop if no material or textures, or if Default-Material
            if (mat == null || !foundTextures || mat.name == defaultMaterial) return;

            //  confirm message
            string message = "";
            if (albedo != null) { message += "   " + albedo.name + "\n"; }
            if (metallic != null) { message += "   " + metallic.name + "\n"; }
            if (normal != null) { message += "   " + normal.name + "\n"; }
            if (height != null) { message += "   " + height.name + "\n"; }
            if (occlusion != null) { message += "   " + occlusion.name + "\n"; }
            if (emission != null) { message += "   " + emission.name + "\n"; }

            if (undo)
                Undo.RecordObject(mat, "Assigned Textures to " + mat.name);

            //  set albedo map
            if (albedo != null)
            {
                float alpha = mat.color.a;
                mat.color = new Color(1, 1, 1, alpha);
                mat.SetTexture("_MainTex", albedo);
            }

            //  set emission map
            if (emission != null)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white);
                mat.SetTexture("_EmissionMap", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
            }

            //  set metallic map
            if (metallic != null)
            {
                mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetTexture("_MetallicGlossMap", metallic);
            }

            //  reimport normal texture as normal map
            if (normal != null)
            {
                if (ETSettings.autoFixNormals)
                {
                    string path = AssetDatabase.GetAssetPath(normal);
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    if (importer.textureType != TextureImporterType.NormalMap)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.SaveAndReimport();
                    }
                }
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normal);
            }

            //  set height map
            if (height != null)
            {
                mat.EnableKeyword("_PARALLAXMAP");
                mat.SetTexture("_ParallaxMap", height);
            }

            //  set occlusion map
            if (occlusion != null)
            {
                mat.SetTexture("_OcclusionMap", occlusion);
            }
        }

        /// <summary>
        /// Clears textures off materials in a list
        /// </summary>
        private static void DoClear(List<Material> mats)
        {
            if (selectedMats.Count <= 0) return;

            foreach (var selectedMat in mats)
            {
                Undo.RecordObject(selectedMat, "Clear Textures off " + selectedMat.name);

                selectedMat.color = Color.white;
                selectedMat.SetTexture("_MainTex", null);
                selectedMat.mainTextureOffset = Vector2.zero;
                selectedMat.mainTextureScale = Vector2.one;

                selectedMat.DisableKeyword("_METALLICGLOSSMAP");
                selectedMat.SetTexture("_MetallicGlossMap", null);

                selectedMat.DisableKeyword("_EMISSION");
                selectedMat.SetTexture("_BumpMap", null);

                selectedMat.DisableKeyword("_NORMALMAP");
                selectedMat.SetTexture("_ParallaxMap", null);

                selectedMat.SetTexture("_OcclusionMap", null);

                selectedMat.DisableKeyword("_DETAIL_MULX2");
                selectedMat.SetTexture("_DetailMask", null);
                selectedMat.SetTexture("_DetailAlbedoMap", null);
                selectedMat.SetTexture("_DetailNormalMap", null);

                selectedMat.DisableKeyword("_EMISSION");
                selectedMat.SetColor("_EmissionColor", Color.clear);
                selectedMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                selectedMat.SetTexture("_EmissionMap", null);
            }
        }


    }


    public class ImportMetallicSmoothness : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!isMetallicOrSmoothness())
            {
                return;
            }

            // Sets some required import values for reading the texture's pixels for combining.
            TextureImporter textureImporter = (TextureImporter)assetImporter;

            textureImporter.isReadable = true;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.mipmapEnabled = false;
        }

        // We need to do the actual combining of textures in the Postprocessor, since the original texture needs to be finished processing first.
        void OnPostprocessTexture(Texture2D texture)
        {
            if (!isMetallicOrSmoothness())
            {
                return;
            }

            string filename = Path.GetFileNameWithoutExtension(assetPath);
            string combinedPath = "";
            string metallicPath = "";
            string smoothnessPath = "";
            
            Texture2D metallic = null;
            Texture2D smoothness = null;
            
            //METALNESS FFS
            if (filename.ToLower().Contains("metalness"))
            {
                Debug.Log("Metalness texture imported");

                metallic = texture;

                smoothnessPath = convertMetallicSmoothnessPath("metalness", "smoothness", out combinedPath);

                if (File.Exists(smoothnessPath))
                {
                    smoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(smoothnessPath);
                }
            }
            else if (filename.ToLower().Contains("metallic") && !filename.ToLower().Contains("metallic-smoothness"))
            {
                Debug.Log("Metallic texture imported");

                metallic = texture;

                smoothnessPath = convertMetallicSmoothnessPath("metallic", "smoothness", out combinedPath);

                if (File.Exists(smoothnessPath))
                {
                    smoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(smoothnessPath);
                }

            }
            else if (filename.ToLower().Contains("roughness"))
            {

                smoothness = texture;

                Debug.Log("Roughness texture imported, inverting color");


                Texture2D inverted = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);

                //invert 
                for (int m = 0; m < texture.mipmapCount; m++)
                {
                    Color[] c = texture.GetPixels(m);

                    for (int i = 0; i < c.Length; i++)
                    {
                        c[i].r = 1 - c[i].r;
                        c[i].g = 1 - c[i].g;
                        c[i].b = 1 - c[i].b;
                    }
                    inverted.SetPixels(c, m);
                }

                string fn = Path.GetFileNameWithoutExtension(assetPath);
                fn = fn.Replace("roughness", "smoothness");
                fn = fn.Replace("Roughness", "smoothness");

                string extension = Path.GetExtension(assetPath);
                string pathWithoutFilename = Path.GetDirectoryName(assetPath);

                string invertedPath = pathWithoutFilename + "/" + fn + extension;

                // Save the combined data.
                byte[] invertedPng = inverted.EncodeToPNG();
                File.WriteAllBytes(invertedPath, invertedPng);
                AssetDatabase.ImportAsset(invertedPath);

                //do as above

                metallicPath = convertMetallicSmoothnessPath("roughness", "metallic", out combinedPath);

                if (File.Exists(metallicPath))
                {
                    metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                }
                else
                {
                    metallicPath = metallicPath.Replace("metallic", "Metallic");
                    if (File.Exists(metallicPath))
                    {
                        metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                    }
                }
            }
            else if (filename.ToLower().Contains("smoothness") && !filename.ToLower().Contains("metallic-smoothness"))
            {
                Debug.Log("Smoothness texture imported");

                smoothness = texture;

                string OGmetallicPath = metallicPath = convertMetallicSmoothnessPath("smoothness", "metallic", out combinedPath);

                if (File.Exists(metallicPath))
                {
                    metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                }
                else
                {
                    //aliases
                    Debug.Log("Trying Metallic");

                    metallicPath = OGmetallicPath.Replace("metallic", "Metallic");
                    if (File.Exists(metallicPath))
                    {
                        metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                    }
                    else
                    {
                        Debug.Log("Trying metalness");

                        metallicPath = OGmetallicPath.Replace("metallic", "metalness");
                        if (File.Exists(metallicPath))
                        {
                            metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                        }
                    }

                }
            }

            if (metallic == null)
            {
                Debug.LogWarningFormat("Associated Metallic texture not found for: {0} - path: {1}", filename, metallicPath);
                return;
            }

            if (smoothness == null)
            {
                Debug.LogWarningFormat("Associated Smoothness texture not found for: {0} - path: {1}", filename, smoothnessPath);
                return;
            }

            if (metallic.width != smoothness.width || metallic.height != smoothness.height)
            {
                Debug.LogWarningFormat("Metallic and Smoothness textures must be the same size in order to combine: {0}", assetPath);
                return;
            }

            var metallicPixels = metallic.GetPixels32();
            var smoothnessPixels = smoothness.GetPixels32();

            Texture2D combined = new Texture2D(metallic.width, metallic.height, TextureFormat.ARGB32, false);

            // Use the red channel info from smoothness for the alpha channel of the combined texture.
            // Since the smoothness should be grayscale, we just use the red channel info.
            for (int i = 0; i < metallicPixels.Length; i++)
            {
                metallicPixels[i].a = smoothnessPixels[i].r;
            }

            combined.SetPixels32(metallicPixels);

            // Save the combined data.
            byte[] png = combined.EncodeToPNG();
            File.WriteAllBytes(combinedPath, png);

            AssetDatabase.ImportAsset(combinedPath);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        // Helper functions.
        ////////////////////////////////////////////////////////////////////////////////////////////////

        // Returns true if the texture being processed ends with " Metallic" or " Smoothness",
        // since we only want to work with those.
        private bool isMetallicOrSmoothness()
        {
            string filename = Path.GetFileNameWithoutExtension(assetPath);

            return ((filename.ToLower().Contains("smoothness") && !filename.ToLower().Contains("metallic-smoothness")) 
                || (filename.ToLower().Contains("metallic") && !filename.ToLower().Contains("metallic-smoothness")) 
                || filename.ToLower().Contains("roughness")
                || filename.ToLower().Contains("metalness"));
        }

        private string convertMetallicSmoothnessPath(string from, string to, out string combinedPath)
        {
            string filename = Path.GetFileNameWithoutExtension(assetPath);
            string extension = Path.GetExtension(assetPath);
            string pathWithoutFilename = Path.GetDirectoryName(assetPath);

            pathWithoutFilename = pathWithoutFilename.Replace("\\", "/");

            //string baseFilename = filename.Replace(from, "");
            //baseFilename = filename.Replace(from.ToLower(), "");
            
            string newFileName = Regex.Replace(filename, from, to, RegexOptions.IgnoreCase);

            //filename.Replace(from, to, System.StringComparison.InvariantCultureIgnoreCase);
            string newPath = pathWithoutFilename + "/"+newFileName + extension;

            string combinedFileName = Regex.Replace(filename, from, "", RegexOptions.IgnoreCase);
            combinedFileName += "_metallic-smoothness.png";

            //string combinedFileName = filename.Replace(from, "_metallic-smoothness.png", System.StringComparison.InvariantCultureIgnoreCase);
            if (combinedFileName[0] == '_')
                combinedFileName = "combined" + combinedFileName;

            combinedPath = pathWithoutFilename +"/"+ combinedFileName;

            Debug.Log("new path " + newPath + " combined path "+ combinedPath);

            //string newPath = string.Format("{0}/{1}{2}{3}", pathWithoutFilename, baseFilename, to, extension);

            //combinedPath = string.Format("{0}/{1}_metallic-smoothness.png", pathWithoutFilename, baseFilename);

            return newPath;
        }
    }

    /// <summary>
    /// Easy Texture Settings Window
    /// </summary>
    public class ETSettings : EditorWindow
    {
        #region VARIABLES
        private Vector2 scrollPos;

        public static string[] ALBEDO_NAMES = { "albedo", "base_color", "color", "diff", "diffuse" };
        public static string[] METALLIC_NAMES = { "metallic", "metalness"};
        public static string[] EMISSION_NAMES = { "emission" };
        public static string[] NORMAL_NAMES = { "normal", "normal_opengl", "nor" }; //nor is risky
        public static string[] HEIGHT_NAMES = { "height" };
        public static string[] OCCLUSION_NAMES = { "occlusion", "AO" };
        public static string[] SMOOTHNESS_NAMES = { "smoothness" };
        public static string[] METALLIC_SMOOTHNESS_NAMES = { "metallic-smoothness" };

        public static bool autoFixNormals = true;
        #endregion
    }
}
