using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenNI;
using NITE;
using System;

public class CubeController : MonoBehaviour {
	private readonly string XML_CONFIG=@".//OpenNI.xml";
	private Context context;
	private ScriptNode scriptNode;
	private DepthGenerator depth;
	private HandsGenerator hands;
	private GestureGenerator gesture;
	private Dictionary<int,List<Point3D>> tracking;
	private int trackingSize = 10;

	void Start () {
		Debug.Log("START APP");
		this.context=Context.CreateFromXmlFile(XML_CONFIG, out scriptNode);
		this.depth=this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;
		if(depth==null){
			throw new Exception("Nodo de Profundidad no encontrado");
		}
		this.hands=this.context.FindExistingNode(NodeType.Hands)as HandsGenerator;
		if(this.hands==null){
			throw new Exception("Nodo de Manos no encontrado");
		}
		this.gesture=this.context.FindExistingNode(NodeType.Gesture)as GestureGenerator;
		if(this.gesture==null){
			throw new Exception("Nodo de Gestos no encontrado");
		}
		//Agregar Handlers
		this.hands.HandCreate+=hands_HandCreate;
		this.hands.HandUpdate+=hands_HandUpdate;
		this.hands.HandDestroy+=hands_HandDestroy;
		
		
		this.gesture.AddGesture("Wave");
		this.gesture.GestureRecognized+=gesture_GestureRecognized;
		this.gesture.StartGenerating();
		tracking=new Dictionary<int, List<Point3D>>();
	}

	
	// Update is called once per frame
	void Update () {
		Debug.Log("Update");
		this.context.WaitOneUpdateAll (this.depth);
		List<int> keys = new List<int>(tracking.Keys);
		foreach(int key in keys){
			//Debug.Log(key);
			List<Point3D> l=tracking[key];
			Debug.Log("X "+l[l.Count-1].X+" Y "+l[l.Count-1].Y+" Z "+l[l.Count-1].Z);
		}
	}
	
	void OnApplicationQuit(){
		Debug.Log("Saliendo de la aplicacion");
		context.Release();
	}
	
	//Handlers-START
	void gesture_GestureRecognized(object sender, GestureRecognizedEventArgs e){
		Debug.Log("Algo --> "+e.Gesture);
		if(e.Gesture=="Wave"){
			this.hands.StartTracking(e.EndPosition);
			Debug.Log("Tracking ");
		}
	}
	
	void hands_HandCreate(object sender, HandCreateEventArgs e){
		//Debug.Log("Create");
		List<Point3D> lista=new List<Point3D>(trackingSize);
		lista.Add(e.Position);
		tracking.Add(e.UserID,lista);
		
	}
	
	void hands_HandUpdate(object sender, HandUpdateEventArgs e){
		//Debug.Log("Update");
		List<Point3D> lista=tracking[e.UserID];
		lista.Add(e.Position);
		if(lista.Count>trackingSize){
			lista.RemoveAt(0);
		}
	}
	
	void hands_HandDestroy(object sender,HandDestroyEventArgs e){
		//Debug.Log("Destroy");
		tracking.Remove(e.UserID);
	}
	//Handlers-END
}
