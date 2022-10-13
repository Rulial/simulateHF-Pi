extends GLTFDocumentExtension
class_name HFCollider


func _import_node(state, _gltf_node, json, node):
	var node_extensions = json.get("extensions")
	if not json.has("extensions"):
		return OK
	if state.base_path.is_empty():
		return OK
	if node_extensions.has("HF_colliders"):
		print("Colliders found.")
		import_agents(state, json, node, node_extensions)
	return OK

func import_agents(state, json, node, extensions):
	var colliders = extensions["HF_colliders"]["objects"]
	var new_node = null
	var old_node = node

	for collider in colliders:
		for key in collider.keys():
			match key:
				"name":
					pass
				"type":
					pass
				"bounding_box":
					pass
				"offset":
					pass
				"intangible":
					pass
				"convex":
					pass
				"physic_material":
					pass

	if new_node:
		node.replace_by(new_node)
		old_node.queue_free()
