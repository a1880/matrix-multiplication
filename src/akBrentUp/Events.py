from util import o, pretty_num

class Events:
    """
    Class to register, count and report events.
    An event is just in occurrence or something denoted by
    a message string. The message string is used a dictionary
    key to keep one count per event category.
    """
    def __init__(self):
        self.events = {}
        
    def register(self, msg: int, times: int = 1) -> None:
        """Increment the event count; start with 0+1 for new event"""
        self.events[msg] = self.events.get(msg, 0) + times
        
    def report(self, purpose: str, prefix: str = "") -> None:
        o(f"{prefix}{purpose}")
        o(f"{prefix}{len(purpose)*'='}")
        
        if not self.events:
            o(f"{prefix}No events to report")
        else:
            key_len = len(max(self.events, key=len)) + 4
            
            for key in sorted(self.events.keys(), key=str.lower):
                o(f"{prefix}{key:{key_len}} x{pretty_num(self.events[key])}")
        
        o(prefix)

