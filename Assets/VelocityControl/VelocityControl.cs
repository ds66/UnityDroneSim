﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityControl : MonoBehaviour {

	public StateFinder state;

	private float gravity = 9.81f;
	private float time_constant_z_velocity = 1.0f; // Normal-person coordinates
	private float time_constant_acceleration = 0.5f;
	private float time_constant_omega_xy_rate = 0.1f; // Normal-person coordinates (roll/pitch)
	private float time_constant_alpha_xy_rate = 0.05f; // Normal-person coordinates (roll/pitch)
	private float time_constant_alpha_z_rate = 0.05f; // Normal-person coordinates (yaw)

	private float max_pitch = 0.175f*0.8f; // 10 Degrees in radians, otherwise small-angle approximation dies 
	private float max_roll = 0.175f*0.8f; // 10 Degrees in radians, otherwise small-angle approximation dies
	private float max_alpha = 10.0f;

	public float desired_vx = 0.0f;
	public float desired_vy = 0.0f;
	public float desired_yaw = 0.0f;
	public float desired_height = 1.0f;

	// Use this for initialization
	void Start () {
		state.GetState ();
		Rigidbody rb = GetComponent<Rigidbody> ();
		Vector3 desiredForce = new Vector3 (0.0f, gravity * state.Mass, 0.0f);
		rb.AddForce (desiredForce, ForceMode.Acceleration);
	}

	// Update is called once per frame
	void Update () {
		state.GetState ();

		// NOTE: I'm using stupid vector order (sideways, up, forward) at the end


		float heightError = state.Altitude - desired_height;


		// relative to object
		Vector3 desiredVelocity = new Vector3(desired_vy, -1.0f * heightError/time_constant_z_velocity, desired_vx);


//		Debug.Log (state.RelativeVelocityVector);
//		Debug.Log (desiredVelocity);

		// relative to object frame


		// project state velocity onto normal so its in our coordinate frame
//		Vector3 normal = transform.forward;
//		Vector3 transVelocityVec = Vector3.Project (state.VelocityVector, normal);

		Quaternion yawRotate = Quaternion.Euler(new Vector3(0.0f, transform.eulerAngles.y, 0.0f));

		//relative to object (not including pitch and roll)
		Vector3 velocityError =  state.VelocityVector - yawRotate * desiredVelocity;

		Vector3 desiredAcceleration = velocityError * -1.0f / time_constant_acceleration;

		Vector3 desiredTheta = new Vector3 (desiredAcceleration.z / gravity, 0.0f, -desiredAcceleration.x / gravity);
		if (desiredTheta.x > max_pitch) {
			desiredTheta.x = max_pitch;
		} else if (desiredTheta.x < -1.0f * max_pitch) {
			desiredTheta.x = -1.0f * max_pitch;
		}
		if (desiredTheta.z > max_roll) {
			desiredTheta.z = max_roll;
		} else if (desiredTheta.z < -1.0f * max_roll) {
			desiredTheta.z = -1.0f * max_roll;
		}

		Vector3 thetaError = state.Angles - desiredTheta;

		Vector3 desiredOmega = thetaError * -1.0f / time_constant_omega_xy_rate;
		desiredOmega.y = desired_yaw;
		Vector3 omegaError = state.AngularVelocityVector - desiredOmega;

		Vector3 desiredAlpha = Vector3.Scale(omegaError, new Vector3(-1.0f/time_constant_alpha_xy_rate, -1.0f/time_constant_alpha_z_rate, -1.0f/time_constant_alpha_xy_rate));
		desiredAlpha = Vector3.Min (desiredAlpha, Vector3.one * max_alpha);
		desiredAlpha = Vector3.Max (desiredAlpha, Vector3.one * max_alpha * -1.0f);

		float desiredThrust = (gravity + desiredAcceleration.y) / (Mathf.Cos (state.Angles.z) * Mathf.Cos (state.Angles.x));

		Vector3 desiredTorque = Vector3.Scale (desiredAlpha, state.Inertia);
//		Vector3 desiredAccel = new Vector3 (0.0f, desiredThrust, 0.0f);
		Vector3 desiredForce = new Vector3 (0.0f, desiredThrust * state.Mass, 0.0f);
//		Vector3 desiredForce = transform.up * desiredThrust * state.Mass;

//		Debug.Log(Vector3.Dot(desiredTorque, transform.forward));


		Rigidbody rb = GetComponent<Rigidbody>();
//		Debug.Log (desiredTorque);
//		Debug.Log (desiredForce);


		Debug.DrawRay (transform.position, desiredTorque * 5.0f, Color.red);
		Debug.DrawRay (transform.position, desiredForce * 5.0f, Color.green);

		rb.AddRelativeTorque (desiredTorque, ForceMode.Acceleration);
		rb.AddRelativeForce (desiredForce , ForceMode.Acceleration);
	}
}
