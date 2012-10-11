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
	
	private Point3D puntoTorso;
	
	private const float MARGEN_CUADRO_CENTRAL=50;
	private const float RADIO_CUADRO_CENTRAL=200;
	private const float RADIO_CUADRO_MOVIL=100;
	
	
	//User Tracking
	private UserGenerator userGenerator;
	private SkeletonCapability skeletonCapability;
	private PoseDetectionCapability poseDetectionCapability;
	private string calibPose;
	private bool shouldRun;
	private Dictionary <int, Dictionary<SkeletonJoint,SkeletonJointPosition>> joints;
	//
	public float valorRotation=0.75f;
	private int manoDr=0;
	private int manoIz=0;
	private Point3D DrRespaldo;
	private Point3D IzRespaldo;
	
	
	void Start () {
		Debug.Log("START APP");
		this.context=Context.CreateFromXmlFile(XML_CONFIG, out scriptNode);
		this.depth=this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;
		if(depth==null){
			throw new Exception("Nodo de Profundidad no encontrado");
		}
	
		
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
		this.joints=new Dictionary<int,Dictionary<SkeletonJoint,SkeletonJointPosition>>();
		//
	}

	
	// Update is called once per frame
	void Update () {
		//Debug.Log("Update");	
		
		this.context.WaitOneUpdateAll (this.depth);
		int[] users=this.userGenerator.GetUsers();
			foreach(int user in users){
				if(this.skeletonCapability.IsTracking(user)){
				updatePuntoRef(skeletonCapability.GetSkeletonJointPosition(user,SkeletonJoint.Torso));
				Point3D handIz,handDr;
				SkeletonJointPosition sjpHandIz=skeletonCapability.GetSkeletonJointPosition(user,SkeletonJoint.RightHand);
				SkeletonJointPosition sjpHandDr=skeletonCapability.GetSkeletonJointPosition(user,SkeletonJoint.LeftHand);
				
				if(isDentroDelCuadro(sjpHandDr.Position)&&isDentroDelCuadro(sjpHandIz.Position)){
					//Debug.Log("Dentro del cuadro");
					/*
					if(sjpHandDr.Position.Z<sjpHandIz.Position.Z){
						transform.Rotate(new Vector3(0,-valorRotation,0));
					}else if(sjpHandIz.Position.Z<sjpHandDr.Position.Z){
						transform.Rotate(new Vector3(0,valorRotation,0));
					}
					*/
					if(sjpHandDr.Position.Y>sjpHandIz.Position.Y){
						transform.Rotate(new Vector3(valorRotation,0,0));
					}else if(sjpHandIz.Position.Y>sjpHandDr.Position.Y){
						transform.Rotate(new Vector3(-valorRotation,0,0));
					}
				}
				
			}
		}
	}
	
	void OnApplicationQuit(){
		Debug.Log("Saliendo de la aplicacion");
		context.Release();
	}	
	
	
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
	
	void updatePuntoRef(SkeletonJointPosition punto){
		this.puntoTorso=punto.Position;
	}
	
	
	bool isDentroDelCuadro(Point3D punto){
		float xCentro=puntoTorso.X;
		float yCentro=puntoTorso.Y;
		float zCentro=puntoTorso.Z;
		bool dentro=false;
		if((punto.X<xCentro+200f&&punto.X>xCentro-200f)&&(punto.Y<yCentro+200f&&punto.Y>yCentro-200f)&&(punto.Z>zCentro-450f&&punto.Z<zCentro-50f)){
			dentro=true;
		}		
		return dentro;
	}
	
	bool isDentro(Point3D puntoAChecar, Point3D puntoCentral, float cantidad, bool isCentral){
		float xCentral=puntoCentral.X;
		float yCentral=puntoCentral.Y;
		float zCentral=puntoCentral.Z;
		bool ret=false;
		if(isCentral){
			if((puntoAChecar.X<xCentral+cantidad&&puntoAChecar.X>xCentral-cantidad)&&(puntoAChecar.Y<yCentral+cantidad&&puntoAChecar.Y>yCentral-cantidad)&&(puntoAChecar.Z>zCentral-((cantidad*2)+MARGEN_CUADRO_CENTRAL)&&(puntoAChecar.Z<zCentral-(MARGEN_CUADRO_CENTRAL)))){
				ret=true;
			}
		}else{
			if((puntoAChecar.X<xCentral+cantidad&&puntoAChecar.X>xCentral-cantidad)&&(puntoAChecar.Y<yCentral+cantidad&&puntoAChecar.Y>yCentral-cantidad)&&(puntoAChecar.Z>zCentral-cantidad&&puntoAChecar.Z<zCentral+cantidad)){
				ret=true;
			}
		}
		
		return ret;
	}
	
	bool isManoDr(Point3D mano){
		bool der=false;
		if(mano.X<puntoTorso.X){
			der=true;
		}		
		return der;
	}

}
