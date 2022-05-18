import simenv as sm


scene = sm.Scene(engine='Unity')

sphere = sm.Sphere('sphere')
light = sm.Light('light')
agent = sm.RL_Agent('agent')
camera = sm.Camera('cam')

scene += sphere
scene += light
scene += agent
scene += camera

scene.build()

scene.close()  # TODO have this in the delete of the Scene class instead of manually
