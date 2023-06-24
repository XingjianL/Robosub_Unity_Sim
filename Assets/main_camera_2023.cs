using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class main_camera_2023 : MonoBehaviour
{
    Camera camera;
    
    int FileCounter;
    static int FileBatch = 100;          // how much to generate with each press
    int FileCap = FileBatch;      
    int imgWidth = 640;     // image properties
    int imgHeight = 480;
    string split = "train"; // sample category ("train", "val", "test")
    
    // SELECT YOUR DATASET
    static int dataSelection = 1;
    // dataset name
    static string[] dataset_ids = { "All",
                                    "Buoy",
                                    "Torpedoes",
                                    "Gate"};
    static int[][] GameObjectClassIDs_Collection = {new int[] {0,1,2,3},
                                                    new int[] {0,1},
                                                    new int[] {0,1,2,3},
                                                    new int[] {0,1,2}};
    static string[][] GameObjectSceneIDs_Collection = { new string[] {"Buoy_1","Buoy_2","Torpedoes_2","Torpedoes_1"},
                                                        new string[] {"Buoy_1","Buoy_2"},
                                                        new string[] {"Torpedoes_1","Torpedoes_2","Torp_1_small","Torp_2_small"},
                                                        new string[] {"Gate_0","Gate_1", "Gate_2"}};
    string dataset_id = dataset_ids[dataSelection];
    static int[] GameObjectClassIDs = GameObjectClassIDs_Collection[dataSelection];
    static string[] GameObjectSceneIDs = GameObjectSceneIDs_Collection[dataSelection];
    
    GameObject[] game_object = new GameObject[GameObjectClassIDs.Length];
    Rect[] goal = new Rect[GameObjectClassIDs.Length];
    bool generate_data = true; // if images should be saved

    // random settings
    // skyboxes
    Material[] skyboxes = new Material[9];

    // coroutines
    private bool spacekeypressed = false;
    void init_rand_settings(){
        for (int i = 0; i < skyboxes.Length; i++){
            string skybox_path = "skyboxes/skybox (" + (i+1).ToString() + ")";
            skyboxes[i] = Resources.Load<Material>(skybox_path);
            //print(skybox_path);
            //print(Resources.Load<Material>(skybox_path));
        }
    }
    // pool rotation

    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;
        for(int i = 0; i < GameObjectClassIDs.Length; i++){
            game_object[i] = GameObject.Find(GameObjectSceneIDs[i]);
        }
        camera.enabled = true;
        print("camera start");
        print(game_object[0].transform.position);

        if (generate_data && camera.targetTexture == null){
            camera.targetTexture = new RenderTexture(imgWidth, imgHeight, 24);
        }
        init_rand_settings();

        // path to dataset storage
        var success = Directory.CreateDirectory(Application.persistentDataPath+"/ML_Generated_Dataset/yolov5/"+dataset_id+"/labels/train");
        Directory.CreateDirectory(Application.persistentDataPath+"/ML_Generated_Dataset/yolov5/"+dataset_id+"/labels/val");
        Directory.CreateDirectory(Application.persistentDataPath+"/ML_Generated_Dataset/yolov5/"+dataset_id+"/labels/test");
        Directory.CreateDirectory(Application.persistentDataPath+"/ML_Generated_Dataset/yolov5/"+dataset_id+"/images/train");
        Directory.CreateDirectory(Application.persistentDataPath+"/ML_Generated_Dataset/yolov5/"+dataset_id+"/images/val");
        Directory.CreateDirectory(Application.persistentDataPath+"/ML_Generated_Dataset/yolov5/"+dataset_id+"/images/test");
        print("Generating Dataset Path");
        print(success);

    }
    private IEnumerator GenerateData()
    {
        print("COROUTINE");
        spacekeypressed = true;
            // generate 10 images and text files for each press
        if (FileCounter < FileCap){
            randomLocation();   // camera position
            randomSkyBox();     // skybox material
            randomRotation();   // pool rotation
            randomRenderOptions();  // engine render settings (fog)
            if (dataSelection != 3){
                randomObjectsProperty(game_object); // game object material rotation
            }
            // global volume settings (filters)
            GameObject global_volume = GameObject.Find("Water Volume");
            WaterPostProcess gvscript = global_volume.GetComponent<WaterPostProcess>();
            gvscript.randomWaterColor();

            for(int i = 0; i < GameObjectClassIDs.Length; i++){
                goal[i] = calcBBoxOnScreen(game_object[i]);
            }
            //trainValTest();
            if (saveTxt()){
                saveImage();
                FileCounter++;
                print("file: " + FileCounter + " " + split);
            }
            //FileCounter++;
            //print(goal);
        }
        if (FileCounter >= FileCap){
            spacekeypressed = false;
            FileCap += FileBatch;
            print("COROUTINE BREAK");
            yield break;
        }
        yield return new WaitForEndOfFrame();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || spacekeypressed) {
            StartCoroutine(GenerateData());
        }

        // get camera location of these vertices
        if (Input.GetKeyDown(KeyCode.P)) {
            trainValTest();
            print(split);
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            randomSkyBox();
            randomRotation();
            randomRenderOptions();
            randomLocation();
            if (dataSelection != 3){
                randomObjectsProperty(game_object);
            }
            // global volume settings (filters)
            GameObject global_volume = GameObject.Find("Water Volume");
            WaterPostProcess gvscript = global_volume.GetComponent<WaterPostProcess>();
            gvscript.randomWaterColor();
        }
    }
    
    void trainValTest(){
        // test, val, train split
        if (split == "val") {
            split = "test";
        } else if (split == "train") {
            split = "val";
        } else {
            split = "train";
        }
    }
    // adjust sampling space (where the camera renders a picture)
    void randomLocation(){
        var randX = UnityEngine.Random.Range(-25.0f, 9.0f);
        var randY = UnityEngine.Random.Range(-4.0f, 6.5f);
        var randZ = UnityEngine.Random.Range(-25.0f, 0.0f);
        Vector3 newLocation = new Vector3(randX, randY, randZ);
        GameObject.Find("Main Camera").transform.position = newLocation;
    }
    void randomSkyBox(){
        int randInt = UnityEngine.Random.Range(0, skyboxes.Length-1);
        RenderSettings.skybox = skyboxes[randInt];
        DynamicGI.UpdateEnvironment();

        //print(skyboxes[randInt]);
    }
    void randomRotation(){
        // not really random
        GameObject swimming_pool = GameObject.Find("exterior_swimming_pool");
        swimming_pool.transform.Rotate(0.0f, 15.0f, 0.0f, Space.World);
        GameObject DecalProjector = GameObject.Find("Decal_Projector");
        DecalProjector.transform.Rotate(0.0f, 10.0f, 0.0f, Space.World);
    }
    void randomRenderOptions(){
        RenderSettings.fogEndDistance = UnityEngine.Random.Range(50, 350);
    }
    void randomObjectsProperty(GameObject[] game_objects){
        for (int i = 0; i < game_objects.Length; i++){
            for (int j = 0; j < game_objects[i].transform.childCount; j++){
                var childObject = game_objects[i].transform.GetChild(j).gameObject;
                var m = childObject.GetComponent<Renderer>().material;
                m.SetFloat("_Rotation", UnityEngine.Random.Range(0, 2*3.1415926f));
                //print(m);
            }
            //for (int j = 0; j < parts.Length; j++){
            //    var m = parts[j].GetComponent<Renderer>().material;
            //    m.SetFloat("_Rotation", UnityEngine.Random.Range(0, 2*3.1415926f));
            //    print(m);
            //}
        }
        
    }
    int checkDataSensible(float center_w, float center_h, float w, float h){
        // center of object off the screen
        if (center_w < 0 || center_w > 1)
            return 0;
        if (center_h < 0 || center_h > 1)
            return 0;
        // too close
        if (w > 1.2 || h > 1.2)
            return 0;
        // object center in the screen, but not entire object (need bound adjustment)
        if (center_w > 0 && center_w < 1 && center_h > 0 && center_h < 1) {
            // left and top bound
            if (center_w < w/2 || center_h < h/2)
                return 2;
            // right and bottom bound
            if ((1-center_w) < w/2 || (1-center_h) < h/2)
                return 2;
        }
        // everything in screen
        return 1;
    }
    float[] boundAdjust(float center_w, float center_h, float w, float h){
        float newcenter_w = center_w;
        float newcenter_h = center_h;
        float newwidth = w;
        float newheight = h;
        float delta;
        // left edge
        if (center_w < w/2){
            delta = (w/2 - center_w);
            newcenter_w = center_w + delta/2;
            newwidth = w - delta;
        }
        // top edge
        if (center_h < h/2){
            delta = (h/2 - center_h);
            newcenter_h = center_h + delta/2;
            newheight = h - delta;
        }
        // right edge
        if ((1-center_w) < w/2){
            delta = (w/2 - (1-center_w));
            newcenter_w = center_w - delta/2;
            newwidth = w - delta;
        }
        // bottom edge
        if ((1-center_h) < h/2){
            delta = (h/2 - (1-center_h));
            newcenter_h = center_h - delta/2;
            newheight = h - delta;
        }
        float[] bounds = {newcenter_w, newcenter_h, newwidth, newheight};
        return bounds;
    }

    void saveImage(){
        // https://forum.unity.com/threads/how-to-save-manually-save-a-png-of-a-camera-view.506269/ 
 
        Texture2D Image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        camera.Render();
        RenderTexture.active = camera.targetTexture;
        Image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        Image.Apply();
        
 
        var Bytes = Image.EncodeToPNG();
        Destroy(Image);
        string save_path = Application.persistentDataPath + "/ML_Generated_Dataset/yolov5/"+dataset_id+"/images/" + split + "/" + FileCounter + ".png";
        print("png: " + save_path);
        if (generate_data) 
            //print("img: " + FileCounter);
            UnityEngine.Windows.File.WriteAllBytes(save_path, Bytes);
    }
    
    bool saveTxt(){
        string dataPoint = "";
        int validDataCount = 0;
        for (int i = 0; i < GameObjectClassIDs.Length; i++){
            var center_w = (goal[i].center.x / imgWidth);
            var center_h = (goal[i].center.y / imgHeight);
            var w = goal[i].width / imgWidth;
            var h = goal[i].height / imgHeight;

            int validData = checkDataSensible(center_w, center_h, w, h);
            if (validData == 2){
                var retBounds = boundAdjust(center_w, center_h, w, h);
                center_w = retBounds[0];
                center_h = retBounds[1];
                w = retBounds[2];
                h = retBounds[3];
            }
            if (validData >= 1){
                if (w > 1)
                    w = 1;
                if (h > 1)
                    h = 1;
                dataPoint += GameObjectClassIDs[i].ToString() + " " + center_w.ToString() + " " + center_h.ToString() + " " + w.ToString() + " " + h.ToString() + "\n";
                validDataCount+=1;
            }
        }
        if (validDataCount > 0){
            string save_path = Application.persistentDataPath + "/ML_Generated_Dataset/yolov5/"+dataset_id+"/labels/"+split+"/"+FileCounter+".txt";
                //print("txt: " + save_path);
                //print(dataPoint);
            var Bytes = System.Text.Encoding.UTF8.GetBytes(dataPoint);
            if (generate_data) 
                UnityEngine.Windows.File.WriteAllBytes(save_path, Bytes);
        }
        return validDataCount > 0;
    }

    Rect calcBBoxOnScreen(GameObject game_object_){
        Collider r = game_object_.GetComponent<Collider>();
        if (r == null)
            print(r);
        
        var bounds = r.bounds;
        //print(bounds);
        // all 8 world vertices of the object
        float[] c1 = {bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z};
        float[] c2 = {bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z};
        float[] c3 = {bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z};
        float[] c4 = {bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z};
        float[] c5 = {bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z};
        float[] c6 = {bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z};
        float[] c7 = {bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z};
        float[] c8 = {bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z};
        Vector3 c1_v = new Vector3(c1[0], c1[1], c1[2]);
        Vector3 c2_v = new Vector3(c2[0], c2[1], c2[2]);
        Vector3 c3_v = new Vector3(c3[0], c3[1], c3[2]);
        Vector3 c4_v = new Vector3(c4[0], c4[1], c4[2]);
        Vector3 c5_v = new Vector3(c5[0], c5[1], c5[2]);
        Vector3 c6_v = new Vector3(c6[0], c6[1], c6[2]);
        Vector3 c7_v = new Vector3(c7[0], c7[1], c7[2]);
        Vector3 c8_v = new Vector3(c8[0], c8[1], c8[2]);
        //print(c1);
        // get camera location of these vertices
        Vector3[] screen_coords = new Vector3[8];
        screen_coords[0] = camera.WorldToScreenPoint(c1_v);
        screen_coords[1] = camera.WorldToScreenPoint(c2_v);
        screen_coords[2] = camera.WorldToScreenPoint(c3_v);
        screen_coords[3] = camera.WorldToScreenPoint(c4_v);
        screen_coords[4] = camera.WorldToScreenPoint(c5_v);
        screen_coords[5] = camera.WorldToScreenPoint(c6_v);
        screen_coords[6] = camera.WorldToScreenPoint(c7_v);
        screen_coords[7] = camera.WorldToScreenPoint(c8_v);
        //print(c1_screen);
                // min/max of x and y locations
        float min_x = 0, min_y = 0;
        float max_x = 0, max_y = 0;
        for (int axis = 0; axis < 3; axis++){
            float max_axis = -1;
            float min_axis = 10000;
            for (int corner = 0; corner < 8; corner++){
                if (screen_coords[corner][axis] > max_axis){
                    max_axis = screen_coords[corner][axis];
                }
                if (screen_coords[corner][axis] < min_axis){
                    min_axis = screen_coords[corner][axis];
                }
            }
            if (axis == 0){
                min_x = min_axis;
                max_x = max_axis;
            } else if (axis == 1){
                min_y = min_axis;
                max_y = max_axis;
            }
        }
        float width = max_x-min_x;
        float height = max_y-min_y;
        //goal = new Rect(min_x,min_y,width,height);

        // [0,0] top left of the screen
        Rect goal_ = new Rect(min_x,imgHeight-max_y,width,height);
        return goal_;
    }

    

    void OnGUI()
    {
        for (int i = 0; i < GameObjectClassIDs.Length; i++){
            GUI.Box(goal[i], "box"+i.ToString());
        }
    }
}
