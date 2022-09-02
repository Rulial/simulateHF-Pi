using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SimEnv.RlAgents {
    public class EnvironmentManager {
        public static EnvironmentManager instance;

        Queue<Environment> environmentQueue = new Queue<Environment>();
        List<Vector3> positionPool = new List<Vector3>();
        List<Environment> activeEnvironments = new List<Environment>();

        float physicsUpdateRate = 1.0f / 30.0f;
        int frameSkip = 4;

        List<SensorBuffer> agentSensorBuffer = new List<SensorBuffer>();
        List<int> obsSizes = new List<int>();
        List<string> sensor_nodes = new List<string>();
        List<string> sensortypes = new List<string>();

        public void Initialize() {
            frameSkip = Client.instance.frameSkip;
            physicsUpdateRate = Client.instance.physicsUpdateRate;
        }

        public void AddToPool(byte[] bytes) {
            GameObject map = GLTF.Importer.LoadFromBytes(bytes);
            Environment environment = new Environment(map);
            environmentQueue.Enqueue(environment);

        }

        public void ActivateEnvironments(int nEnvironments) {
            if (nEnvironments == -1) {
                nEnvironments = environmentQueue.Count;
            }
            CreatePositionPool(nEnvironments);

            Debug.Assert(nEnvironments <= environmentQueue.Count);
            for (int i = 0; i < nEnvironments; i++) {
                Environment environment = environmentQueue.Dequeue();

                environment.Reset();
                environment.SetPosition(positionPool[i]);
                environment.Enable();
                activeEnvironments.Add(environment);
            }

            obsSizes = activeEnvironments[0].GetObservationSizes();
            sensor_nodes = activeEnvironments[0].GetSensorNames();
            sensortypes = activeEnvironments[0].GetSensorTypes();

            for (int i = 0; i < obsSizes.Count; i++) {
                agentSensorBuffer.Add(new SensorBuffer(nEnvironments * obsSizes[i], sensortypes[i]));
            }

            frameSkip = Client.instance.frameSkip;
            physicsUpdateRate = Client.instance.physicsUpdateRate;
        }

        private void CreatePositionPool(int nEnvironments) {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var env in environmentQueue) {
                Bounds envBounds = env.bounds;
                bounds.Encapsulate(envBounds);
            }

            Vector3 step = bounds.extents * 2f + new Vector3(1f, 0f, 1f);

            int count = 0;
            int root = Convert.ToInt32(Math.Ceiling(Math.Sqrt(Convert.ToDouble(nEnvironments))));
            bool stop = false;
            for (int i = 0; i < root; i++) {
                if (stop) break;
                for (int j = 0; j < root; j++) {
                    if (stop) break;
                    positionPool.Add(new Vector3(Convert.ToSingle(i) * step.x, 0f, Convert.ToSingle(j) * step.z));

                    count++;
                    if (count == nEnvironments) {
                        stop = true;
                    }
                }
            }
            Debug.Assert(count == nEnvironments);

        }

        public void Step(List<List<float>> actions) {
            for (int j = 0; j < frameSkip; j++) {
                for (int i = 0; i < activeEnvironments.Count; i++) {
                    activeEnvironments[i].Step(actions[i], physicsUpdateRate);
                }
                // sim calls the physics update
                Simulator.Step(1, physicsUpdateRate);
                // update rewards
                for (int i = 0; i < activeEnvironments.Count; i++) {
                    activeEnvironments[i].UpdateReward();
                }
            }
        }

        public void ResetEnvironments() {
            for (int i = 0; i < activeEnvironments.Count; i++) {
                ResetAt(i);
            }
        }

        public void ResetAt(int i) {
            activeEnvironments[i].Disable();
            environmentQueue.Enqueue(activeEnvironments[i]);
            activeEnvironments[i] = environmentQueue.Dequeue();
            activeEnvironments[i].Reset();
            activeEnvironments[i].SetPosition(positionPool[i]);
            activeEnvironments[i].Enable();
        }
        public float[] GetReward() {
            List<float> rewards = new List<float>();
            for (int i = 0; i < activeEnvironments.Count; i++) {
                rewards.Add(activeEnvironments[i].GetReward());
                activeEnvironments[i].ZeroReward();
            }
            return rewards.ToArray<float>();
        }

        public bool[] GetDone(bool autoReset = true) {
            // Check if the agent is in a terminal state 
            // TODO: add option for auto reset
            List<bool> dones = new List<bool>();
            for (int i = 0; i < activeEnvironments.Count; i++) {
                bool done = activeEnvironments[i].GetDone();
                dones.Add(done);
                if (done && autoReset) {
                    ResetAt(i);
                }
            }
            return dones.ToArray<bool>();

        }

        public void GetObservation(UnityAction<string> callback) {
            GetObservationCoroutine(callback).RunCoroutine();
        }
        private IEnumerator GetObservationCoroutine(UnityAction<string> callback) {
            List<int[]> obsShapes = activeEnvironments[0].GetObservationShapes();
            List<int[]> shapesWithAgents = new List<int[]>();

            for (int j = 0; j < obsShapes.Count; j++) {
                int[] obsShape = obsShapes[j];
                int[] shapeWithAgents = new int[obsShape.Length + 1];
                shapeWithAgents[0] = activeEnvironments.Count;
                Array.Copy(obsShape, 0, shapeWithAgents, 1, obsShape.Length); // copy the old values
                shapesWithAgents.Add(shapeWithAgents);
            }

            List<Coroutine> coroutines = new List<Coroutine>();

            for (int i = 0; i < activeEnvironments.Count; i++) {
                Coroutine coroutine = activeEnvironments[i].GetObservationCoroutine(agentSensorBuffer, obsSizes, i).RunCoroutine();
                coroutines.Add(coroutine);
            }

            foreach (var coroutine in coroutines) {
                yield return coroutine;
            }
            // flatten the two arrays
            List<string> strings = new List<string>();
            for (int j = 0; j < obsShapes.Count; j++) {

                string string_array = agentSensorBuffer[j].ToJson(shapesWithAgents[j], sensor_nodes[j]);
                strings.Add(string_array);

            }
            callback(JsonHelper.ToJson(strings.ToArray()));
        }

    }

}