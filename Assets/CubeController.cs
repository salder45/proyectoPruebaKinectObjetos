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
	
	private const float MEDIDA_SEGURIDAD_FRENTE_Z=50;
	private const float MEDIDA_SEGURIDAD_CENTRAL=350;
	private const float MEDIDA_SEGURIDAD_MANOS=100;
	
	
	//User Tracking
	private UserGenerator userGenerator;
	private SkeletonCapability skeletonCapability;
	private PoseDetectionCapability poseDetectionCapability;
	private string calibPose;
	private bool shouldRun;
	private Dictionary <int, Dictionary<SkeletonJoint,SkeletonJointPosition>> joints;
	//
	public float valorRotation=0.75f;
	private int tamanoList=2;
	private List<float> distancias;
			
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
		distancias=new List<float>(tamanoList);
	}

	
	// Update is called once per frame
	void Update () {
		//Debug.Log("Update");	
		
		this.context.WaitOneUpdateAll (this.depth);
		int[] users=this.userGenerator.GetUsers();
			foreach(int user in users){
			
			if(this.skeletonCapability.IsTracking(user)){
				updatePuntoRef(skeletonCapability.GetSkeletonJointPosition(user,SkeletonJoint.Torso));
				SkeletonJointPosition posHandIz=skeletonCapability.GetSkeletonJointPosition(user,SkeletonJoint.RightHand);
				SkeletonJointPosition posHandDr=skeletonCapability.GetSkeletonJointPosition(user,SkeletonJoint.LeftHand);
				
				if(isDentroCuadroSeguridad(posHandDr.Position)&isDentroCuadroSeguridad(posHandIz.Position)){
					distancias.Add(distanciaEntreDosPuntos(posHandIz.Position,posHandDr.Position));
					if(distancias.Count>tamanoList){
						distancias.RemoveAt(0);
					}
					
					
					
					if(distancias.Count==2){
						float ultimo=distancias[1];
						float primero=distancias[0];
						
						if(ultimo>primero){
							Debug.Log("Aumentar");
							transform.localScale=new Vector3(2,2,2);
						}else if(ultimo<primero){
							Debug.Log("Disminuir");
							transform.localScale=new Vector3(0.5f,0.5f,0.5f);
						}
					}
					
					
					/*
					float x,y,z;
					x=y=z=0;									
					if((posHandDr.Position.Z<posHandIz.Position.Z&isDentroMargenY(posHandDr.Position.Y,posHandIz.Position.Y))&(!isDentroMargenZ(posHandDr.Position.Z,posHandIz.Position.Z))){
						y=-valorRotation;
					}else if((posHandDr.Position.Z>posHandIz.Position.Z&isDentroMargenY(posHandDr.Position.Y,posHandIz.Position.Y))&(!isDentroMargenZ(posHandDr.Position.Z,posHandIz.Position.Z))){
						y=valorRotation;
					}
					
					if((posHandDr.Position.Y<posHandIz.Position.Y&isDentroMargenZ(posHandDr.Position.Z,posHandIz.Position.Z))&(!isDentroMargenY(posHandDr.Position.Y,posHandIz.Position.Y))){
						x=-valorRotation;
					}else if((posHandDr.Position.Y>posHandIz.Position.Y&isDentroMargenZ(posHandDr.Position.Z,posHandIz.Position.Z))&(!isDentroMargenY(posHandDr.Position.Y,posHandIz.Position.Y))){
						x=valorRotation;
					}
					transform.Rotate(new Vector3(x,y,z));
					*/
				}
				
				
				//x
				//positivo es hacia arriba
				//negativo hacia abajo
				
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
	
	bool isDentroCuadroSeguridad(Point3D puntoMano){
		bool retorno=false;
		float x,y,z;
		x=puntoTorso.X;
		y=puntoTorso.Y;
		z=puntoTorso.Z;
		
		if((x+MEDIDA_SEGURIDAD_CENTRAL>puntoMano.X&&x-MEDIDA_SEGURIDAD_CENTRAL<puntoMano.X)&&
			(y+MEDIDA_SEGURIDAD_CENTRAL>puntoMano.Y&&y-MEDIDA_SEGURIDAD_CENTRAL<puntoMano.Y)&&
			(z-((2*MEDIDA_SEGURIDAD_CENTRAL)+MEDIDA_SEGURIDAD_FRENTE_Z)<puntoMano.Z&&z-MEDIDA_SEGURIDAD_FRENTE_Z>puntoMano.Z)){
			retorno=true;
		}	
		
		return retorno;
	}
	
	bool isDentroMargenX(float xManoDr, float xManoIz){
		bool retorno=false;
		if(xManoDr+MEDIDA_SEGURIDAD_MANOS>=xManoIz&&xManoDr-MEDIDA_SEGURIDAD_MANOS<=xManoIz){
			retorno=true;
		}
		return retorno;
	}
	
	bool isDentroMargenY(float yManoDr, float yManoIz){
		bool retorno=false;
		if(yManoDr+MEDIDA_SEGURIDAD_MANOS>=yManoIz&&yManoDr-MEDIDA_SEGURIDAD_MANOS<=yManoIz){
			retorno=true;
		}
		return retorno;
	}
	
	bool isDentroMargenZ(float zManoDr, float zManoIz){
		bool retorno=false;
		if(zManoDr+MEDIDA_SEGURIDAD_MANOS>zManoIz&&zManoIz>zManoDr-MEDIDA_SEGURIDAD_MANOS){
			retorno=true;
		}
		return retorno;
	}
	
	float distanciaEntreDosPuntos(Point3D a,Point3D b){
		return Mathf.Sqrt(elevaCuadrado(b.X-a.X)+elevaCuadrado(b.Y-a.Y)+elevaCuadrado(b.Z-a.Z));
	}
	
	float elevaCuadrado(float numero){
		return (float)Math.Pow(numero,2);
	}
}
