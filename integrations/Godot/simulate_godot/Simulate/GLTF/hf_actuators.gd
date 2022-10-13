extends GLTFDocumentExtension
class_name HFActuators


func _import_node(state: GLTFState, _gltf_node: GLTFNode, json_data: Dictionary, node: Node):
	var node_extensions = json_data.get("extensions")
	if not json_data.has("extensions"):
		return OK
	if state.base_path.is_empty():
		return OK
	print(node_extensions)
	if node_extensions is not null:
		print(node_extensions)
		print(state)
	#	print(node_extensions.keys())
		if node_extensions.has("HF_actuators"):
			print("Actuators found.")
			import_actuators(state, json_data, node, node_extensions)
	return OK

func import_actuators(state, json, node, extensions):
	print("OI ZEBI?????????????????????????????????????????")
	var actuators = extensions["HF_actuators"]["objects"]
	var new_node = null
	var old_node = node

	for actuator in actuators:
		var hf_actuator: HFActuator = HFActuator.new()
		new_node = hf_actuator
		
		for key in actuator.keys():
			print(key)
			match key:
				"mapping":
					new_node.mapping = ActionMapping.new()
					for mapping_key in actuator["mapping"].keys():
						match mapping_key:
							"action":
								new_node.mapping.action = actuator["mapping"]["action"]
							"amplitude":
								new_node.mapping.amplitude = actuator["mapping"]["amplitude"]
							"offset":
								new_node.mapping.offset = actuator["mapping"]["offset"]
							"axis":
								new_node.mapping.axis = actuator["mapping"]["axis"]
							"position":
								new_node.mapping.position = actuator["mapping"]["position"]
							"use_local_coordinates":
								new_node.mapping.use_local_coordinates = actuator["mapping"]["use_local_coordinates"]
							"is_impulse":
								new_node.mapping.is_impulse = actuator["mapping"]["is_impulse"]
							"max_velocity_threshold":
								new_node.mapping.max_velocity_threshold = actuator["mapping"]["max_velocity_threshold"]
				"n":
					new_node.n = actuator["n"]
				"dtype":
					new_node.dtype = actuator["dtype"]
				"low":
					new_node.low = actuator["low"]
				"high":
					new_node.high = actuator["high"]
				"shape":
					new_node.shape = actuator["shape"]
				_:
					print("Field not implemented")

	if new_node:
		node.replace_by(new_node)
		old_node.queue_free()


class HFActuator:
	extends Node
	var mapping: ActionMapping
	var n: int
	var dtype: String
	var low: Array
	var high: Array
	var shape: Array


class ActionMapping:
	var action: String
	var amplitude: float
	var offset: float
	var axis: Vector3
	var position: Vector3
	var use_local_coordinates: bool
	var is_impulse: bool
	var max_velocity_threshold: float


class ActionSpace:
	var action_map: Array

	func _init(mapping: Array):
		self.action_map = mapping

	func get_mapping(key) -> ActionMapping:
		var index: int = int(key)
		return self.action_map[index]
