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
    int img_choice = 0;
    string[] resolutions =new string[] {"640x480", "640x640", "1280x960", "1280x1280", "1920x1080", "320x320"};
    string split = "train"; // sample category ("train", "val", "test")
    // SELECT YOUR DATASET (Change this)
    static int dataSelection = 4; // which one in dataset_ids (start at 0)
    bool camSelection = false; // true: front camera, false: down camera
    // dataset name
    static string[] dataset_ids = { "All (not work)",
                                    "Buoy",
                                    "Torpedoes (not work)",
                                    "Gate",
                                    "Bins"};
    static int[][] GameObjectClassIDs_Collection = {new int[] {0,1,2,3},
                                                    new int[] {0,1,2,3},
                                                    new int[] {0,1,2,3},
                                                    new int[] {0,1,2,1,2},
                                                    new int[] {0,0,0,1,2,1,2,1,2}
                                                    };
    static string[][] GameObjectSceneIDs_Collection = { new string[] {"Buoy_1","Buoy_2","Torpedoes_2","Torpedoes_1"},
                                                        new string[] {"earthbuoy1","earthbuoy2","abydoesbuoy1","abydoesbuoy2"},
                                                        new string[] {"Torpedoes_1","Torpedoes_2","Torp_1_small","Torp_2_small"},
                                                        new string[] {"Gate_0","Gate_1", "Gate_2", "Gate_3", "Gate_4"},
                                                        new string[] {"Bin_Cover_1","Bin_Cover_2","Bin_Cover_3","Bin_1", "Bin_2","Bin_3","Bin_4","Bin_5","Bin_6"},
                                                        };
    string dataset_id = dataset_ids[dataSelection];
    static int[] GameObjectClassIDs = GameObjectClassIDs_Collection[dataSelection];
    static string[] GameObjectSceneIDs = GameObjectSceneIDs_Collection[dataSelection];
    
    GameObject[] game_object = new GameObject[GameObjectClassIDs.Length];
    Rect[] goal = new Rect[GameObjectClassIDs.Length];
    bool generate_data = true; // if images should be saved

    // resources
    static string[] pool_base_colors = new string[] {"pool_base_1","pool_base_2","pool_base_3","pool_base_4"};
    static string[] pool_normals = new string[] {"pool_norm_1","pool_norm_2","pool_norm_3","pool_norm_4"};
    // random settings
    // skyboxes
    Material[] skyboxes = new Material[9];
    // water
    GameObject global_volume;
    WaterPostProcess gvscript;
    private void swapModes(){
        dataSelection += 1;
        if (dataSelection >= dataset_ids.Length){
            dataSelection = 0;
        }
        if (dataSelection == 4){
            camSelection = false;
        } else {
            camSelection = true;
        }
        dataset_id = dataset_ids[dataSelection];
        GameObjectClassIDs = GameObjectClassIDs_Collection[dataSelection];
        GameObjectSceneIDs = GameObjectSceneIDs_Collection[dataSelection];
    
        game_object = new GameObject[GameObjectClassIDs.Length];
        goal = new Rect[GameObjectClassIDs.Length];
        print("Swapped to "+dataset_id);
    }
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

        // camera forward/downward
        if (dataSelection > 3) {
            camSelection = false;
        } else { camSelection = true;}
        global_volume = GameObject.Find("Water Volume");
        gvscript = global_volume.GetComponent<WaterPostProcess>();
    }
    private IEnumerator GenerateData()
    {
        print("COROUTINE");
        spacekeypressed = true;
            // generate 10 images and text files for each press
        if (FileCounter < FileCap){
            
            for(int i = 0; i < GameObjectClassIDs.Length; i++){
                goal[i] = calcBBoxOnScreen(game_object[i]);
            }
            //trainValTest();
            if (saveTxt()){
                saveImage();
                FileCounter++;
                print("file: " + FileCounter + " " + split);
            }
            randomRoutine();

            //FileCounter++;
            //print(goal);
        }
        if (FileCounter >= FileCap){
            spacekeypressed = false;
            FileCap += FileBatch;
            print("COROUTINE BREAK");
            yield break;
        }
        yield return new WaitForSeconds(0.1f);
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
            for(int i = 0; i < GameObjectClassIDs.Length; i++){
                goal[i] = calcBBoxOnScreen(game_object[i]);
            }

            
        }
        if (Input.GetKeyDown(KeyCode.T)){
            for(int i = 0; i < GameObjectClassIDs.Length; i++){
                goal[i] = calcBBoxOnScreen(game_object[i]);
            }
        }
        if (Input.GetKeyDown(KeyCode.S)){
            swapModes();
            Start();
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
    void randomRoutine(){
        randomSkyBox();
        randomTexture();    // pool texture
        randomRotation();
        randomRenderOptions();
        randomLocation(camSelection);
        randomObjectsProperty(game_object);
        // global volume settings (filters)
        gvscript.randomWaterColor();

        var randFOV = UnityEngine.Random.Range(40, 102);
        var randFocalLength = UnityEngine.Random.Range(15.0f, 25.0f);
        camera.fieldOfView = randFOV;
        camera.focalLength = randFocalLength;
        //randomCameraConfig(camera);
        var display_cam = GameObject.Find("Main_Camera_Display").GetComponent<Camera>();
        display_cam.fieldOfView = randFOV;
        display_cam.focalLength = randFocalLength;
    }

    void randomTexture(){
        var pool_mat = GameObject.Find("structure").GetComponent<Renderer>().materials;
        var randBase = UnityEngine.Random.Range(0, 3);
        var randNorm = UnityEngine.Random.Range(0, 3);
        var base_texture = Resources.Load<Texture2D>(pool_base_colors[randBase]);
        var norm_texture = Resources.Load<Texture2D>(pool_normals[randNorm]);
        foreach (var m in pool_mat){
            //print(m.name);
            if (m.name.Contains("esp_tile_color1")){
                //print(string.Join(", ", m.GetTexturePropertyNames()));
                m.SetTexture("_BaseMap", base_texture);
                m.SetTexture("_BumpMap", norm_texture);
            }
        }
    }
    // adjust sampling space (where the camera renders a picture)
    void randomLocation(bool Front){
        var randX = UnityEngine.Random.Range(-25.0f, 9.0f);
        if (dataSelection == 3){
            randX += 8;
        }
        var randY = UnityEngine.Random.Range(-6.0f, 6.5f);
        var randZ = UnityEngine.Random.Range(-25.0f, 0.0f);
        var randX_Rot = UnityEngine.Random.Range(-12.0f, 12.0f);
        var randY_Rot = UnityEngine.Random.Range(-45.0f, 45.0f);
        var randZ_Rot = UnityEngine.Random.Range(-20.0f, 20.0f);
        if (!Front) {
            randX = UnityEngine.Random.Range(-18.0f, 1.0f);
            randY = UnityEngine.Random.Range(-2.0f, 7f);
            randZ = UnityEngine.Random.Range(-30.0f, -10.0f);
            randX_Rot = UnityEngine.Random.Range(80.0f, 100.0f);
            //randY_Rot = UnityEngine.Random.Range(-45.0f, 45.0f);
            randZ_Rot = UnityEngine.Random.Range(-45.0f, 45.0f);
        }

        Vector3 newLocation = new Vector3(randX, randY, randZ);
        Vector3 newRotation = new Vector3(randX_Rot, randY_Rot, randZ_Rot);
        GameObject.Find("Main Camera").transform.position = newLocation;
        GameObject.Find("Main Camera").transform.rotation = Quaternion.Euler(newRotation);
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
        GameObject world_light = GameObject.Find("Directional Light");
        var randX_Rot = UnityEngine.Random.Range(45.0f, 135.0f);
        var randY_Rot = UnityEngine.Random.Range(-60.0f, 60.0f);
        world_light.transform.rotation = Quaternion.Euler(new Vector3(randX_Rot, randY_Rot, 0.0f));
    }
    void randomRenderOptions(){
        RenderSettings.fogEndDistance = UnityEngine.Random.Range(50, 350);
    }
    void randomObjectsProperty(GameObject[] game_objects){
        if (dataSelection == 1){
            for (int i = 0; i < game_objects.Length; i++){
            //for (int j = 0; j < game_objects[i].transform.childCount; j++){
            //    var childObject = game_objects[i].transform.GetChild(j).gameObject;
            //    var m = childObject.GetComponent<Renderer>().material;
            //    m.SetFloat("_Rotation", UnityEngine.Random.Range(0, 2*3.1415926f));
            //    
            //    //print(m);
            //}
                var _m = game_objects[i].GetComponent<Renderer>().material;
                _m.SetFloat("_Rotation", UnityEngine.Random.Range(0, 2*3.1415926f));
            //for (int j = 0; j < parts.Length; j++){
            //    var m = parts[j].GetComponent<Renderer>().material;
            //    m.SetFloat("_Rotation", UnityEngine.Random.Range(0, 2*3.1415926f));
            //    print(m);
            //}
            }
        }
        else if (dataSelection == 4) {
            for (int i = 0; i < game_objects.Length; i++){
                //print(game_objects[i]);
                //print(game_objects[i].name);
                if (game_objects[i].name == "Bin_Cover_1" || game_objects[i].name == "Bin_Cover_2"){
                    game_objects[i].transform.localPosition = new Vector3(0, 1.6f, UnityEngine.Random.Range(-2.0f, 2.0f));
                }
            }
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
        //for (int i = 0; i < 8; i++){
        //print(screen_coords[i]);
        //}
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
        print(goal_.ToString());
        return goal_;
    }
    void changeCamResolution(Camera cam){
        cam.targetTexture = new RenderTexture(imgWidth, imgHeight, 24);
    }
    

    void OnGUI()
    {
        var display_cam = GameObject.Find("Main_Camera_Display").GetComponent<Camera>();
        int display_width = display_cam.pixelWidth;
        int display_height = display_cam.pixelHeight;
        float w_ratio = (float)display_width / imgWidth;
        float h_ratio = (float)display_height / imgHeight;
        //changeCamResolution(display_cam, imgWidth, imgHeight);
        var button_rect = new Rect(display_width/20, display_height/20, display_width/4, 40);
        GUI.Label(new Rect(10, display_height-40, display_width, 20),
            "Total Images Generated: " + FileCounter + ". Display: "+display_width+"x"+display_height + ". Image: "+imgWidth+"x"+imgHeight);

        // buttons
        if (GUI.Button(button_rect, "Generate " + dataset_id)){
            StartCoroutine(GenerateData());
        }
        button_rect.y += 50;
        if (GUI.Button(button_rect, "Change data gen \n(now: " + dataset_id + ")")){
            swapModes();
            Start();
        }
        button_rect.y += 50;
        if (GUI.Button(button_rect, "Change Split \n(now: " + split + ")")){
            trainValTest();
            print(split);
        }
        button_rect.y += 50;
        if (GUI.Button(button_rect, "Demo randomization")){
            for(int i = 0; i < GameObjectClassIDs.Length; i++){
                goal[i] = calcBBoxOnScreen(game_object[i]);
            }

            randomRoutine();
        }
        button_rect.y += 50;
        //button_rect.height = button_rect.height * 2;
        if (GUI.Button(button_rect, "Demo Bounding Box\nNeed same aspect ratio")){
            for(int i = 0; i < GameObjectClassIDs.Length; i++){
                goal[i] = calcBBoxOnScreen(game_object[i]);
            }
        }
        button_rect.y += 50;
        button_rect.height = button_rect.height * 1.5f;
        int new_choice = GUI.SelectionGrid(button_rect, img_choice, resolutions,2);
        if (new_choice != img_choice){
            img_choice = new_choice;
            print("Changed Image Resolution");
            switch (img_choice){
                case 0:
                imgWidth = 640;
                imgHeight = 480;
                break;
                case 1:
                imgWidth = 640;
                imgHeight = 640;
                break;
                case 2:
                imgWidth = 1280;
                imgHeight = 960;
                break;
                case 3:
                imgWidth = 1280;
                imgHeight = 1280;
                break;
                case 4:
                imgWidth =1920;
                imgHeight = 1080;
                break;
                case 5:
                imgWidth = 320;
                imgHeight = 320;
                break;
            }
            changeCamResolution(camera);
        }
        
        for (int i = 0; i < GameObjectClassIDs.Length; i++){
            Rect _goal = new Rect(0,0,0,0);
            _goal.x = goal[i].x * w_ratio;
            _goal.y = goal[i].y * h_ratio;
            _goal.width = goal[i].width * w_ratio;
            _goal.height = goal[i].height * h_ratio;

            GUI.Box(_goal, "box"+i.ToString());
        }
        
        
    }
}
