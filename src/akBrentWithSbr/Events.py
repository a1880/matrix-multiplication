class Events:
    """
    Class to register, count and report events.
    An event is just in occurrence or something denoted by
    a message string. The message string is used a dictionary
    key to keep one count per event category.
    """
    def __init__(self):
        self.events = {}
        
    def register(self, msg, times=1):
        """Increment the event count; start with 0+1 for new event"""
        self.events[msg] = self.events.get(msg, 0) + times
        
    def report(self, purpose):
        print()
        print(f"{purpose}")
        print(f"{len(purpose)*'='}")
        
        key_len = len(max(self.events, key=len)) + 4
        
        for key in sorted(self.events.keys()):
            print(f"{key:{key_len}} x{self.events[key]}")
        
        print()
