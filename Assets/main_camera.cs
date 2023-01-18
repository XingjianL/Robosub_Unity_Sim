using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main_camera : MonoBehaviour
{
    Camera camera;
    public GameObject Buoy;
    //public Transform target;
    Rect goal;
    int FileCounter;
    string dataset_id = "train1";
    int game_object_class_id = 0;
    bool generate_data = true;
    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;
        Buoy = GameObject.Find("Buoy_TommyGun");
        camera.enabled = true;
        print("camera start");
        print(Buoy.transform.position);

        if (generate_data & camera.targetTexture == null){
            print(1);
            camera.targetTexture = new RenderTexture(640, 480, 24);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            print("Space Pressed");
            goal = calcBBoxOnScreen(Buoy);
            saveImage();
            saveTxt();
            print(goal);
        }
        // get camera location of these vertices


        
        // convert to yolo format (bounds image to [0,1])
        // label_class, center_x, center_y, half_width, half_height
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
        string save_path = Application.dataPath + "/ML_Generated_Dataset/yolov5/"+dataset_id+"/images/" + FileCounter + ".png";
        print("png: " + save_path);
        if (generate_data) 
            UnityEngine.Windows.File.WriteAllBytes(save_path, Bytes);
        FileCounter++;
    }
    
    void saveTxt(){
        print(goal.center);
        print(goal.height);
        print(goal.width);
        string save_path = Application.dataPath + "/ML_Generated_Dataset/yolov5/"+dataset_id+"/labels/" + FileCounter + ".txt";
        print("txt: " + save_path);
    }

    Rect calcBBoxOnScreen(GameObject game_object){
        Collider r = game_object.GetComponent<Collider>();
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
        goal = new Rect(min_x,480-max_y,width,height);
        return goal;
    }

    

    void OnGUI()
    {
        GUI.Box(goal, "box");
    }
}
