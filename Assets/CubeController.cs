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
	
	
	
	//User Tracking
	private UserGenerator userGenerator;
	private SkeletonCapability skeletonCapability;
	private PoseDetectionCapability poseDetectionCapability;
	private string calibPose;
	private bool shouldRun;
	private Dictionary <int, Dictionary<SkeletonJoint,SkeletonJointPosition>> joints;
	//

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
		
		
		//User Tracking
		this.userGenerator=new UserGenerator(this.context);
		this.skeletonCapability=this.userGenerator.SkeletonCapability;
		this.poseDetectionCapability=this.userGenerator.PoseDetectionCapability;
		this.calibPose=this.skeletonCapability.CalibrationPose;
		this.userGenerator.NewUser+=userGenerator_NewUser;
		this.userGenerator.LostUser+=userGenerator_LostUser;
		this.poseDetectionCapability.PoseDetected+=poseDetectionCapability_PoseDetected;
		this.skeletonCapability.CalibrationComplete+=skeletonCapability_CalibrationComplete;		
		this.skeletonCapability.SetSkeletonProfile(SkeletonProfile.All);
		this.userGenerator.StartGenerating();
		this.shouldRun=true;
		this.joints=new Dictionary<int,Dictionary<SkeletonJoint,SkeletonJointPosition>>();
		//
	}

	
	// Update is called once per frame
	void Update () {
		Debug.Log("Update");
		
		
		this.context.WaitOneUpdateAll (this.depth);
		calcular();
		
		int[] users=this.userGenerator.GetUsers();
			foreach(int user in users){
				if(this.skeletonCapability.IsTracking(user)){
				SkeletonJointPosition punto=this.skeletonCapability.GetSkeletonJointPosition(user,SkeletonJoint.Torso);
				Debug.Log("Punto Medio es: X"+punto.Position.X+" Y: "+punto.Position.Y+" Z: "+punto.Position.Z);
			}
		}
		
		
		/*
		List<int> keys = new List<int>(tracking.Keys);
		foreach(int key in keys){
			//Debug.Log(key);
			List<Point3D> l=tracking[key];
			Debug.Log("X "+l[l.Count-1].X+" Y "+l[l.Count-1].Y+" Z "+l[l.Count-1].Z);
		}
		*/
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
		Debug.Log("Create");
		List<Point3D> lista=new List<Point3D>(trackingSize);
		lista.Add(e.Position);
		tracking.Add(e.UserID,lista);
		
	}
	
	void hands_HandUpdate(object sender, HandUpdateEventArgs e){
		Debug.Log("Update");
		List<Point3D> lista=tracking[e.UserID];
		lista.Add(e.Position);
		if(lista.Count>trackingSize){
			lista.RemoveAt(0);
		}
	}
	
	void hands_HandDestroy(object sender,HandDestroyEventArgs e){
		Debug.Log("Destroy");
		tracking.Remove(e.UserID);
	}
	
	void calcular(){
		List<int> keys = new List<int>(tracking.Keys);
		if(keys.Count==2){
			
			
			foreach(int key in keys){
				
			}			
		}else{
			Debug.Log("No se puede mover");
		}
	}
	//Handlers-END
	
	
	
	//User Generator Handlers
	void userGenerator_NewUser(object sender, NewUserEventArgs e){
          if (this.skeletonCapability.DoesNeedPoseForCalibration){
            	this.poseDetectionCapability.StartPoseDetection(this.calibPose, e.ID);
           }else{
            	this.skeletonCapability.RequestCalibration(e.ID, true);
            }
    }
	
	void poseDetectionCapability_PoseDetected(object sender, PoseDetectedEventArgs e){
            this.poseDetectionCapability.StopPoseDetection(e.ID);
            this.skeletonCapability.RequestCalibration(e.ID, true);
    }
	
	void skeletonCapability_CalibrationComplete(object sender, CalibrationProgressEventArgs e){
            if (e.Status == CalibrationStatus.OK){
                this.skeletonCapability.StartTracking(e.ID);
                this.joints.Add(e.ID, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
            }else if (e.Status != CalibrationStatus.ManualAbort){
                if (this.skeletonCapability.DoesNeedPoseForCalibration){
                    this.poseDetectionCapability.StartPoseDetection(calibPose, e.ID);
                }else{
                    this.skeletonCapability.RequestCalibration(e.ID, true);
                }
            }
    }
	
	void userGenerator_LostUser(object sender, UserLostEventArgs e){
		this.joints.Remove(e.ID);
	}
	
}
